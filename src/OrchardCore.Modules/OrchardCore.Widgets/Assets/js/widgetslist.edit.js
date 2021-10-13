$(function () {
    $(document).on('click', '.add-list-widget', function (event) {
        var type = $(this).data("widget-type");
        var targetId = $(this).data("target-id");
        var htmlFieldPrefix = $(this).data("html-field-prefix");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var prefixesName = $(this).data("prefixes-name");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var zonesName = $(this).data("zones-name");
        var zone = $(this).data("zone");
        // Use a prefix based on the items count (not a guid) so that the browser autofill still works.
        var prefix = htmlFieldPrefix + '-' + zone + '-' + $('#' + targetId + " .widget-editor-body").length.toString();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&zonesName=" + zonesName + "&zone=" + zone + "&targetId=" + targetId + "&parentContentType=" + parentContentType + "&partName=" + partName
        })
        .done(function (data) {
            var result = JSON.parse(data);
            $(document.getElementById(targetId)).append(result.Content);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
        });
    });

    $(document).on('click', '.insert-list-widget', function (event) {
        var type = $(this).data("widget-type");
        var target = $(this).closest('.widget-template');
        var targetId = $(this).data("target-id");
        var htmlFieldPrefix = $(this).data("html-field-prefix");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var prefixesName = $(this).data("prefixes-name");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var zonesName = $(this).data("zones-name");
        var zone = $(this).data("zone");
        // Use a prefix based on the items count (not a guid) so that the browser autofill still works.
        var prefix = htmlFieldPrefix + '-' + zone + '-' + $('#' + targetId + " .widget-editor-body").length.toString();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&zonesName=" + zonesName + "&zone=" + zone + "&targetId=" + targetId + "&parentContentType=" + parentContentType + "&partName=" + partName
        })
        .done(function (data) {
            var result = JSON.parse(data);
            $(result.Content).insertBefore(target);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
        });
    });

    $(document).on('click', '.widget-list-delete', function () {
        var $this = $(this);
        confirmDialog(_objectSpread({}, $this.data(), {
            callback: function callback(r) {
                if (r) {
                    $this.closest('.widget-template').remove();
                    $(document).trigger('contentpreview:render');
                }
            }
        }));
    });

    $(document).on('change', '.widget-editor-footer label', function () {
        $(document).trigger('contentpreview:render');
    });

    $(document).on('click', '.widget-list-editor-btn-toggle', function () {
        $(this).closest('.widget-editor').toggleClass('collapsed');
    });

});
