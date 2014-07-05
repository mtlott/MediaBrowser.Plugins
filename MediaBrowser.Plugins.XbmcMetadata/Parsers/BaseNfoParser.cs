﻿using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Plugins.XbmcMetadata.Parsers
{
    public class BaseNfoParser<T>
        where T : BaseItem
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoParser{T}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BaseNfoParser(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void Fetch(T item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentNullException();
            }

            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            //Fetch(item, metadataFile, settings, Encoding.GetEncoding("ISO-8859-1"), cancellationToken);
            Fetch(item, metadataFile, settings, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Fetch(T item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            using (var streamReader = new StreamReader(metadataFile, encoding))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            FetchDataFromXmlNode(reader, item);
                        }
                    }
                }
            }
        }

        protected virtual void FetchDataFromXmlNode(XmlReader reader, T item)
        {
            switch (reader.Name)
            {
                // DateCreated
                case "dateadded":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTime added;
                            if (DateTime.TryParse(val, out added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else
                            {
                                Logger.Warn("Invalid Added value found: " + val);
                            }
                        }
                        break;
                    }

                case "title":
                case "originaltitle":
                case "localtitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "criticrating":
                    {
                        var text = reader.ReadElementContentAsString();

                        var hasCriticRating = item as IHasCriticRating;

                        if (hasCriticRating != null && !string.IsNullOrEmpty(text))
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasCriticRating.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "budget":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Budget = value;
                            }
                        }

                        break;
                    }

                case "revenue":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Revenue = value;
                            }
                        }

                        break;
                    }

                case "metascore":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasMetascore = item as IHasMetascore;
                        if (hasMetascore != null)
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasMetascore.Metascore = value;
                            }
                        }

                        break;
                    }

                case "awardsummary":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasAwards = item as IHasAwards;
                        if (hasAwards != null)
                        {
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                hasAwards.AwardSummary = text;
                            }
                        }

                        break;
                    }

                case "sorttitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ForcedSortName = val;
                        }
                        break;
                    }

                case "outline":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var hasShortOverview = item as IHasShortOverview;

                            if (hasShortOverview != null)
                            {
                                hasShortOverview.ShortOverview = val;
                            }
                        }
                        break;
                    }

                case "biography":
                case "plot":
                case "review":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "criticratingsummary":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var hasCriticRating = item as IHasCriticRating;

                            if (hasCriticRating != null)
                            {
                                hasCriticRating.CriticRatingSummary = val;
                            }
                        }

                        break;
                    }

                case "language":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasLanguage = item as IHasPreferredMetadataLanguage;
                        if (hasLanguage != null)
                        {
                            hasLanguage.PreferredMetadataLanguage = val;
                        }

                        break;
                    }

                case "website":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.HomePageUrl = val;
                        }

                        break;
                    }

                case "lockedfields":
                    {
                        var fields = new List<MetadataFields>();

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var list = val.Split('|').Select(i =>
                            {
                                MetadataFields field;

                                if (Enum.TryParse<MetadataFields>(i, true, out field))
                                {
                                    return (MetadataFields?)field;
                                }

                                return null;

                            }).Where(i => i.HasValue).Select(i => i.Value);

                            fields.AddRange(list);
                        }

                        item.LockedFields = fields;

                        break;
                    }

                case "tagline":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasTagline = item as IHasTaglines;
                        if (hasTagline != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasTagline.AddTagline(val);
                            }
                        }
                        break;
                    }

                case "country":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasProductionLocations = item as IHasProductionLocations;
                        if (hasProductionLocations != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasProductionLocations.AddProductionLocation(val);
                            }
                        }
                        break;
                    }

                case "mpaa":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }
                        break;
                    }

                case "mpaadescription":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRatingDescription = rating;
                        }
                        break;
                    }

                case "customrating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }
                        break;
                    }

                case "runtime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, _usCulture, out runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "aspectratio":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasAspectRatio = item as IHasAspectRatio;
                        if (!string.IsNullOrWhiteSpace(val) && hasAspectRatio != null)
                        {
                            hasAspectRatio.AspectRatio = val;
                        }
                        break;
                    }

                case "lockdata":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }
                        break;
                    }

                case "studio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddStudio(val);
                        }
                        break;
                    }

                case "director":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }
                case "writer":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "actor":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            var person = GetPersonFromXmlNode(subtree);

                            item.AddPerson(person);
                        }
                        break;
                    }

                case "trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasTrailer = item as IHasTrailers;
                        if (hasTrailer != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasTrailer.AddTrailerUrl(val, false);
                            }
                        }
                        break;
                    }

                case "displayorder":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasDisplayOrder = item as IHasDisplayOrder;
                        if (hasDisplayOrder != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasDisplayOrder.DisplayOrder = val;
                            }
                        }
                        break;
                    }

                case "year":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int productionYear;
                            if (int.TryParse(val, out productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
                            }
                        }

                        break;
                    }

                case "rating":
                    {

                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            float val;
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "aired":
                case "formed":
                case "premiered":
                case "releasedate":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate.ToUniversalTime();
                                item.ProductionYear = airDate.Year;
                            }
                        }

                        break;
                    }

                case "enddate":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.EndDate = airDate.ToUniversalTime();
                            }
                        }

                        break;
                    }

                case "tvdbid":
                    var tvdbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tvdbId))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, tvdbId);
                    }
                    break;

                case "votes":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                            {
                                item.VoteCount = num;
                            }
                        }
                        break;
                    }
                case "musicbrainzalbumid":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzAlbum, mbz);
                        }
                        break;
                    }
                case "musicbrainzalbumartistid":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, mbz);
                        }
                        break;
                    }
                case "musicbrainzartistid":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzArtist, mbz);
                        }
                        break;
                    }
                case "musicbrainzreleasegroupid":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, mbz);
                        }
                        break;
                    }
                case "tvrageid":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.TvRage, id);
                        }
                        break;
                    }
                case "audiodbartistid":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.AudioDbArtist, id);
                        }
                        break;
                    }
                case "audiodbalbumid":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.AudioDbAlbum, id);
                        }
                        break;
                    }
                case "rottentomatoesid":
                    var rtId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(rtId))
                    {
                        item.SetProviderId(MetadataProviders.RottenTomatoes, rtId);
                    }
                    break;

                case "tmdbid":
                    var tmdb = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdb);
                    }
                    break;

                case "collectionnumber":
                    var tmdbCollection = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdbCollection))
                    {
                        item.SetProviderId(MetadataProviders.TmdbCollection, tmdbCollection);
                    }
                    break;

                case "tvcomid":
                    var TVcomId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(TVcomId))
                    {
                        item.SetProviderId(MetadataProviders.Tvcom, TVcomId);
                    }
                    break;

                case "zap2itid":
                    var zap2ItId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(zap2ItId))
                    {
                        item.SetProviderId(MetadataProviders.Zap2It, zap2ItId);
                    }
                    break;

                case "imdb_id":
                case "imdbid":
                    var imDbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(imDbId))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, imDbId);
                    }
                    break;

                case "genre":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddGenre(val);
                        }
                        break;
                    }

                case "style":
                case "tag":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var hasTags = item as IHasTags;
                            if (hasTags != null)
                            {
                                hasTags.AddTag(val);
                            }
                        }
                        break;
                    }

                case "plotkeyword":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasKeywords = item as IHasKeywords;
                        if (hasKeywords != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasKeywords.AddKeyword(val);
                            }
                        }
                        break;
                    }

                case "fileinfo":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromFileInfoNode(subtree, item);
                        }
                        break;
                    }

                default:
                    reader.Skip();
                    break;
            }
        }

        private void FetchFromFileInfoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "streamdetails":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromStreamDetailsNode(subtree, item);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private void FetchFromStreamDetailsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "video":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromVideoNode(subtree, item);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private void FetchFromVideoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "format3d":
                            {
                                var video = item as Video;

                                if (video != null)
                                {
                                    var val = reader.ReadElementContentAsString();

                                    if (string.Equals("HSBS", val, StringComparison.CurrentCulture))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfSideBySide;
                                    }
                                    else if (string.Equals("HTAB", val, StringComparison.CurrentCulture))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                                    }
                                    else if (string.Equals("FTAB", val, StringComparison.CurrentCulture))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                                    }
                                    else if (string.Equals("FSBS", val, StringComparison.CurrentCulture))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullSideBySide;
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private PersonInfo GetPersonFromXmlNode(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "type":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    type = val;
                                }
                                break;
                            }

                        case "role":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }
                                break;
                            }
                        case "sortorder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int intVal;
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out intVal))
                                    {
                                        sortOrder = intVal;
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder
            };
        }

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            value = value ?? string.Empty;

            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = value.IndexOf('|') == -1 && value.IndexOf(';') == -1 ? new[] { ',' } : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? new string[] { } : Split(value, separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Provides an additional overload for string.split
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="separators">The separators.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        private static string[] Split(string val, char[] separators, StringSplitOptions options)
        {
            return val.Split(separators, options);
        }
    }
}