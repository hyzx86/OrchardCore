using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Features.Models;
using OrchardCore.Features.ViewModels;
using OrchardCore.Routing;

namespace OrchardCore.Features.Controllers
{
    public class AdminController : Controller
    {
        private readonly IExtensionManager _extensionManager;
        private readonly IShellFeaturesManager _shellFeaturesManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ShellSettings _shellSettings;
        private readonly INotifier _notifier;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;

        public AdminController(
            IExtensionManager extensionManager,
            IHtmlLocalizer<AdminController> localizer,
            IShellFeaturesManager shellFeaturesManager,
            IAuthorizationService authorizationService,
            ShellSettings shellSettings,
            INotifier notifier,
            IStringLocalizer<AdminController> stringLocalizer)
        {
            _extensionManager = extensionManager;
            _shellFeaturesManager = shellFeaturesManager;
            _authorizationService = authorizationService;
            _shellSettings = shellSettings;
            _notifier = notifier;
            H = localizer;
            S = stringLocalizer;
        }

        public async Task<ActionResult> Features()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageFeatures))
            {
                return Forbid();
            }

            var enabledFeatures = await _shellFeaturesManager.GetEnabledFeaturesAsync();
            var alwaysEnabledFeatures = await _shellFeaturesManager.GetAlwaysEnabledFeaturesAsync();

            var moduleFeatures = new List<ModuleFeature>();

            var features = (await _shellFeaturesManager.GetAvailableFeaturesAsync()).Where(f => !f.IsTheme());

            foreach (var moduleFeatureInfo in features)
            {
                var dependentFeatures = _extensionManager.GetDependentFeatures(moduleFeatureInfo.Id);
                var featureDependencies = _extensionManager.GetFeatureDependencies(moduleFeatureInfo.Id);

                var moduleFeature = new ModuleFeature
                {
                    Descriptor = moduleFeatureInfo,
                    IsEnabled = enabledFeatures.Contains(moduleFeatureInfo),
                    IsAlwaysEnabled = alwaysEnabledFeatures.Contains(moduleFeatureInfo),
                    //IsRecentlyInstalled = _moduleService.IsRecentlyInstalled(f.Extension),
                    //NeedsUpdate = featuresThatNeedUpdate.Contains(f.Id),
                    EnabledDependentFeatures = dependentFeatures.Where(x => x.Id != moduleFeatureInfo.Id && enabledFeatures.Contains(x)).ToList(),
                    FeatureDependencies = featureDependencies.Where(d => d.Id != moduleFeatureInfo.Id).ToList()
                };

                moduleFeatures.Add(moduleFeature);
            }

            return View(new FeaturesViewModel
            {
                Features = moduleFeatures
            });
        }

        [HttpPost]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> Features(BulkActionViewModel model, bool? force)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageFeatures))
            {
                return Forbid();
            }

            if (model.FeatureIds == null || !model.FeatureIds.Any())
            {
                ModelState.AddModelError(nameof(BulkActionViewModel.FeatureIds), S["Please select one or more features."]);
            }

            if (ModelState.IsValid)
            {
                var features = (await _shellFeaturesManager.GetAvailableFeaturesAsync())
                    .Where(f => !f.IsTheme() && model.FeatureIds.Contains(f.Id));

                await EnableOrDisableFeaturesAsync(features, model.BulkAction, force);
            }

            return RedirectToAction(nameof(Features));
        }

        [HttpPost]
        public async Task<IActionResult> Disable(string id)
        {
            var feature = (await _shellFeaturesManager.GetAvailableFeaturesAsync())
                .FirstOrDefault(f => !f.IsTheme() && f.Id == id);

            if (feature == null)
            {
                return NotFound();
            }

            // Generating routes can fail while the tenant is recycled as routes can use services.
            // It could be fixed by waiting for the next request or the end of the current one
            // to actually release the tenant. Right now we render the url before recycling the tenant.

            var nextUrl = Url.Action(nameof(Features));

            await EnableOrDisableFeaturesAsync(new[] { feature }, FeaturesBulkAction.Disable, force: true);

            return Redirect(nextUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Enable(string id)
        {
            var feature = (await _shellFeaturesManager.GetAvailableFeaturesAsync())
                .FirstOrDefault(f => !f.IsTheme() && f.Id == id);

            if (feature == null)
            {
                return NotFound();
            }

            // Generating routes can fail while the tenant is recycled as routes can use services.
            // It could be fixed by waiting for the next request or the end of the current one
            // to actually release the tenant. Right now we render the url before recycling the tenant.

            var nextUrl = Url.Action(nameof(Features));

            await EnableOrDisableFeaturesAsync(new[] { feature }, FeaturesBulkAction.Enable, force: true);

            return Redirect(nextUrl);
        }

        private async Task EnableOrDisableFeaturesAsync(IEnumerable<IFeatureInfo> features, FeaturesBulkAction action, bool? force)
        {
            switch (action)
            {
                case FeaturesBulkAction.None:
                    break;
                case FeaturesBulkAction.Enable:
                    await _shellFeaturesManager.EnableFeaturesAsync(features, force == true);
                    await NotifyAsync(features);
                    break;
                case FeaturesBulkAction.Disable:
                    await _shellFeaturesManager.DisableFeaturesAsync(features, force == true);
                    await NotifyAsync(features, enabled: false);
                    break;
                case FeaturesBulkAction.Toggle:
                    // The features array has already been checked for validity.
                    var enabledFeatures = await _shellFeaturesManager.GetEnabledFeaturesAsync();
                    var disabledFeatures = await _shellFeaturesManager.GetDisabledFeaturesAsync();
                    var featuresToEnable = disabledFeatures.Intersect(features);
                    var featuresToDisable = enabledFeatures.Intersect(features);

                    await _shellFeaturesManager.UpdateFeaturesAsync(featuresToDisable, featuresToEnable, force == true);
                    await NotifyAsync(featuresToEnable);
                    await NotifyAsync(featuresToDisable, enabled: false);
                    return;
                default:
                    break;
            }
        }

        private async ValueTask NotifyAsync(IEnumerable<IFeatureInfo> features, bool enabled = true)
        {
            foreach (var feature in features)
            {
                await _notifier.SuccessAsync(H["{0} was {1}.", feature.Name ?? feature.Id, enabled ? "enabled" : "disabled"]);
            }
        }
    }
}
