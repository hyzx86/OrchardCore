using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Apis.GraphQL;
using OrchardCore.Media.Fields;

namespace OrchardCore.Media.GraphQL
{
    public class MediaFieldQueryObjectType : ObjectGraphType<MediaField>
    {
        public MediaFieldQueryObjectType()
        {
            Name = nameof(MediaField);

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>()
                .Name("paths")
                .Description("the media paths")
                .PagingArguments()
                .Resolve(x =>
                {
                    if (x.Source?.Paths is null)
                    {
                        return Array.Empty<string>();
                    }
                    return x.Page(x.Source.Paths);
                });

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>()
                .Name("fileNames")
                .Description("the media fileNames")
                .PagingArguments()
                .Resolve(x =>
                {
                    var fileNames = x.Source?.GetAttachedFileNames();
                    if (fileNames is null)
                    {
                        return Array.Empty<string>();
                    }
                    return x.Page(fileNames);
                });

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>()
                .Name("urls")
                .Description("the absolute urls of the media items")
                .PagingArguments()
                .Resolve(x =>
                {
                    if (x.Source?.Paths is null)
                    {
                        return Array.Empty<string>();
                    }
                    var paths = x.Page(x.Source.Paths);
                    var mediaFileStore = x.RequestServices.GetService<IMediaFileStore>();
                    return paths.Select(p => mediaFileStore.MapPathToPublicUrl(p));
                });

            Field<ListGraphType<MediaFieldFileItemType>, IEnumerable<MediaFieldFileItem>>()
                .Name("files")
                .Description("the files of the media items")
                .PagingArguments()
                .Resolve(x =>
                {
                    if (x.Source?.Paths is null)
                    {
                        return Array.Empty<MediaFieldFileItem>();
                    }

                    var paths = x.Page(x.Source.Paths).ToArray();
                    var mediaFileStore = x.RequestServices.GetService<IMediaFileStore>();
                    var urls = paths.Select(p => mediaFileStore.MapPathToPublicUrl(p)).ToArray();
                    var fileNames = x.Page(x.Source?.GetAttachedFileNames()).ToArray();
                    var mediaTexts = x.Source?.MediaTexts ?? [];

                    var items = new List<MediaFieldFileItem>();
                    for (int i = 0; i < paths.Count(); i++)
                    {
                        items.Add(new MediaFieldFileItem
                        {
                            Path = paths[i],
                            FileName = fileNames.Length > i ? fileNames[i] : string.Empty,
                            Url = urls.Length > i ? urls[i] : string.Empty,
                            MediaText = mediaTexts.Length > i ? mediaTexts[i] : string.Empty,
                        });
                    }

                    return items;
                });

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>("mediatexts")
                .Description("the media texts")
                .PagingArguments()
                .Resolve(x =>
                {
                    if (x.Source?.MediaTexts is null)
                    {
                        return Array.Empty<string>();
                    }
                    return x.Page(x.Source.MediaTexts);
                });
        }
    }

    public class MediaFieldFileItemType : ObjectGraphType<MediaFieldFileItem>
    {
        public MediaFieldFileItemType()
        {
            Field<StringGraphType>("fileName", "the file name of the media file item", resolve: x => x.Source.FileName);
            Field<StringGraphType>("path", "the path of the media file item", resolve: x => x.Source.Path);
            Field<StringGraphType>("url", "the url name of the media file item", resolve: x => x.Source.Url);
            Field<StringGraphType>("mediaText", "the media text of the media file item", resolve: x => x.Source.MediaText);
        }
    }
    public class MediaFieldFileItem
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string MediaText { get; set; }
    }
}
