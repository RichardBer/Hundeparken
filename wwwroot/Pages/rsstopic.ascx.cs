/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2010 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System.Text.RegularExpressions;

namespace YAF.Pages
{
    // YAF.Pages
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Text;
    using System.Web;
    using System.Xml;

    using YAF.Classes;
    using YAF.Classes.Core;
    using YAF.Classes.Data;
    using YAF.Classes.Utils;

    #endregion

    /// <summary>
    /// Summary description for rss topic.
    /// </summary>
    public partial class rsstopic : ForumPage
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="rsstopic"/> class.
        /// </summary>
        public rsstopic()
            : base("RSSTOPIC")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// The page_ load.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // Put user code to initialize the page here 
            if (!(PageContext.BoardSettings.ShowRSSLink || PageContext.BoardSettings.ShowAtomLink))
            {
                YafBuildLink.RedirectInfoPage(InfoMessage.AccessDenied);
            }

            // Atom feed as variable
            bool atomFeedByVar = Request.QueryString.GetFirstOrDefault("ft") ==
                                  YafSyndicationFormats.Atom.ToInt().ToString();

            YafSyndicationFeed feed = null;
            var syndicationItems = new List<SyndicationItem>();
            string lastPostIcon = BaseUrlBuilder.BaseUrl + PageContext.CurrentForumPage.GetThemeContents("ICONS", "ICON_NEWEST");
            string lastPostName = this.PageContext.Localization.GetText("DEFAULT", "GO_LAST_POST");

            YafRssFeeds feedType = YafRssFeeds.Forum;

            try
            {
                feedType = this.Request.QueryString.GetFirstOrDefault("pg").ToEnum<YafRssFeeds>(true);
            }
            catch
            {
                // default to Forum Feed.
            }

            switch (feedType)
            {
                // Latest posts feed
                case YafRssFeeds.LatestPosts:
                    if (!(this.PageContext.BoardSettings.ShowActiveDiscussions && this.Get<YafPermissions>().Check(PageContext.BoardSettings.PostLatestFeedAccess)))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    GetPostLatestFeed(ref feed, feedType, atomFeedByVar, lastPostIcon, lastPostName);
                    break;

                // Latest Announcements feed
                case YafRssFeeds.LatestAnnouncements:
                    if (!this.Get<YafPermissions>().Check(PageContext.BoardSettings.ForumFeedAccess))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    GetLatestAnnouncementsFeed(ref feed, feedType, atomFeedByVar);
                    break;

                // Posts Feed
                case YafRssFeeds.Posts:
                    if (!(this.PageContext.ForumReadAccess && this.Get<YafPermissions>().Check(PageContext.BoardSettings.PostsFeedAccess)))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    if (this.Request.QueryString.GetFirstOrDefault("t") != null)
                    {
                        int topicId;
                        if (int.TryParse(this.Request.QueryString.GetFirstOrDefault("t"), out topicId))
                        {
                            GetPostsFeed(ref feed, feedType, atomFeedByVar, topicId);
                        }
                    }

                    break;

                // Forum Feed
                case YafRssFeeds.Forum:
                    if (!this.Get<YafPermissions>().Check(PageContext.BoardSettings.ForumFeedAccess))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    object categoryId = null;

                    if (this.Request.QueryString.GetFirstOrDefault("c") != null)
                    {
                        int icategoryId = 0;
                        if (int.TryParse(this.Request.QueryString.GetFirstOrDefault("c"), out icategoryId))
                        {
                            categoryId = icategoryId;
                        }
                    }

                    GetForumFeed(ref feed, feedType, atomFeedByVar, categoryId);
                    break;

                // Topics Feed
                case YafRssFeeds.Topics:
                    if (!(this.PageContext.ForumReadAccess && this.Get<YafPermissions>().Check(PageContext.BoardSettings.TopicsFeedAccess)))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    int forumId;
                    if (this.Request.QueryString.GetFirstOrDefault("f") != null)
                    {
                        if (int.TryParse(this.Request.QueryString.GetFirstOrDefault("f"), out forumId))
                        {
                            GetTopicsFeed(ref feed, feedType, atomFeedByVar, lastPostIcon, lastPostName, forumId);
                        }
                    }
                    break;

                // Active Topics
                case YafRssFeeds.Active:
                    if (!this.Get<YafPermissions>().Check(PageContext.BoardSettings.ActiveTopicFeedAccess))
                    {
                        YafBuildLink.AccessDenied();
                    }

                    int categoryActiveIntId;
                    object categoryActiveId = null;
                    if (this.Request.QueryString.GetFirstOrDefault("f") != null && int.TryParse(this.Request.QueryString.GetFirstOrDefault("f"), out categoryActiveIntId))
                    {
                        categoryActiveId = categoryActiveIntId;
                    }

                    GetActiveFeed(ref feed, feedType, atomFeedByVar, lastPostIcon, lastPostName, categoryActiveId);

                    break;
                case YafRssFeeds.Favorite:
                    if (!this.Get<YafPermissions>().Check(PageContext.BoardSettings.FavoriteTopicFeedAccess))
                    {
                        YafBuildLink.AccessDenied();
                    }
                    int categoryFavIntId;
                    object categoryFavId = null;
                    if (this.Request.QueryString.GetFirstOrDefault("f") != null && int.TryParse(this.Request.QueryString.GetFirstOrDefault("f"), out categoryFavIntId))
                    {
                        categoryFavId = categoryFavIntId;
                    }

                    GetFavoriteFeed(ref feed, feedType, atomFeedByVar, lastPostIcon, lastPostName, categoryFavId);
                    break;
                default:
                    YafBuildLink.AccessDenied();
                    break;
            }

            // update the feed with the item list... 
            // the list should be added after all other feed properties are set
            if (feed != null)
            {
                var writer = new XmlTextWriter(this.Response.OutputStream, Encoding.UTF8);
                writer.WriteStartDocument();

                // write the feed to the response writer);
                if (!atomFeedByVar)
                {
                    var rssFormatter = new Rss20FeedFormatter(feed);
                    rssFormatter.WriteTo(writer);
                    this.Response.ContentType = "application/rss+xml";
                }
                else
                {
                    var atomFormatter = new Atom10FeedFormatter(feed);
                    atomFormatter.WriteTo(writer);

                    this.Response.ContentType = "application/atom+xml";
                }
               
                writer.WriteEndDocument();
                writer.Close();

                this.Response.ContentEncoding = Encoding.UTF8;
                this.Response.Cache.SetCacheability(HttpCacheability.Public);

                this.Response.End();
            }
            else
            {
                YafBuildLink.RedirectInfoPage(InfoMessage.AccessDenied);
            }
        }

        /// <summary>
        /// The method to return latest topic content to display in a feed.
        /// </summary>
        /// <param name="link">A linkt to an active topic.</param>
        /// <param name="imgUrl">A latest topic icon Url.</param>
        /// <param name="imgAlt">A latest topic icon Alt text.</param>
        /// <param name="linkName">A latest topic displayed link name</param>
        /// <param name="text">An active topic first message content/partial content.</param>
        /// <param name="flags"></param>
        /// <returns>An Html formatted first message content string.</returns>
        private static string GetPostLatestContent(string link, string imgUrl, string imgAlt, string linkName, string text, int flags, bool altItem)
        {
            text = YafFormatMessage.FormatSyndicationMessage(text, new MessageFlags(flags), altItem, 4000);
            return @"{0}<table><tr><td><a href=""{1}"" ><img src=""{2}"" alt =""{3}"" title =""{3}"" />&nbsp;{4}</a></td></tr><table>".FormatWith(text, link, imgUrl, imgAlt, linkName);
          
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for topics in a forum.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="lastPostIcon">The icon for last post link.</param>
        /// <param name="lastPostName">The last post name.</param>
        private void GetPostLatestFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, string lastPostIcon, string lastPostName)
        {
            var syndicationItems = new List<SyndicationItem>();

            using (DataTable dataTopics = DB.rss_topic_latest(this.PageContext.PageBoardID, this.PageContext.BoardSettings.ActiveDiscussionsCount <= 50 ? this.PageContext.BoardSettings.ActiveDiscussionsCount : 50, PageContext.PageUserID, PageContext.BoardSettings.UseStyledNicks, PageContext.BoardSettings.NoCountForumsInActiveDiscussions))
            {
                string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);

                feed = new YafSyndicationFeed(this.PageContext.Localization.GetText("ACTIVE_DISCUSSIONS"), feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);
                bool altItem = false;
                foreach (DataRow row in dataTopics.Rows)
                {
                    // don't render moved topics
                    if (row["TopicMovedID"].IsNullOrEmptyDBField())
                    {
                        DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) + this.Get<YafDateTime>().TimeOffset;
                        if (syndicationItems.Count <= 0)
                        {
                            feed.LastUpdatedTime = lastPosted + this.Get<YafDateTime>().TimeOffset;
                            feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                            Convert.ToInt64(row["UserID"])));
                        
                        }

                        feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                             Convert.ToInt64(
                                                                                                 row["LastUserID"])));

                        string messageLink = YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "m={0}#post{0}",
                                                                            row["LastMessageID"]);

                        syndicationItems.AddSyndicationItem(
                            row["Topic"].ToString(),
                            GetPostLatestContent(messageLink, lastPostIcon, lastPostName, lastPostName, row["LastMessage"].ToString(),
                                                 !row["LastMessageFlags"].IsNullOrEmptyDBField()
                                                     ? Convert.ToInt32(row["LastMessageFlags"])
                                                     : 22, altItem),
                            null,
                            YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "t={0}", Convert.ToInt32(row["TopicID"])),
                           "urn:{0}:ft{1}:st{2}:tid{3}:mid{4}:{5}".FormatWith(urlAlphaNum,
                           feedType,
                           atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                           row["TopicID"],
                           row["LastMessageID"], PageContext.PageBoardID), 
                           lastPosted,
                           feed);
                    }

                    altItem = !altItem;
                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for topic announcements.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        private void GetLatestAnnouncementsFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar)
        {
            var syndicationItems = new List<SyndicationItem>();
            using (DataTable dt = DB.topic_announcements(this.PageContext.PageBoardID, 10, this.PageContext.PageUserID))
            {
                string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);

                feed = new YafSyndicationFeed(this.PageContext.Localization.GetText("POSTMESSAGE", "ANNOUNCEMENT"), feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

                foreach (DataRow row in dt.Rows)
                {
                    // don't render moved topics
                    if (row["TopicMovedID"].IsNullOrEmptyDBField())
                    {
                        DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) + this.Get<YafDateTime>().TimeOffset;

                        if (syndicationItems.Count <= 0)
                        {
                            feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                            Convert.ToInt64(row["UserID"])));
                            feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;
                            
                        }

                        feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                             Convert.ToInt64(
                                                                                                 row["LastUserID"])));

                        syndicationItems.AddSyndicationItem(
                            row["Subject"].ToString(),
                            row["Message"].ToString(),
                            null,
                            YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "t={0}",
                                                           this.Request.QueryString.GetFirstOrDefault("t")),
                            "urn:{0}:ft{1}:st{2}:tid{3}:lmid{4}:{5}".FormatWith(urlAlphaNum, 
                            feedType,
                            atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                            this.Request.QueryString.GetFirstOrDefault("t"),
                            row["LastMessageID"], PageContext.PageBoardID),
                            lastPosted, 
                            feed);
                    }
                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for posts.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="topicId">The TopicID</param>
        private void GetPostsFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, int topicId)
        {
            var syndicationItems = new List<SyndicationItem>();
            using (
                 DataTable dt = DB.post_list(
                   topicId, 0, this.PageContext.BoardSettings.ShowDeletedMessages, false))
            {
                // convert to linq...
                var rowList = dt.AsEnumerable().OrderByDescending(x => x.Field<DateTime>("Posted"));
               
                // see if the deleted messages need to be edited out...)
                if (this.PageContext.BoardSettings.ShowDeletedMessages && !this.PageContext.BoardSettings.ShowDeletedMessagesToAll &&
                    !this.PageContext.IsAdmin && !this.PageContext.IsForumModerator)
                {
                    // remove posts that are deleted and do not belong to this user...
                    rowList =
                    rowList.Where(x => !(x.Field<bool>("IsDeleted") && x.Field<int>("UserID") != this.PageContext.PageUserID)).OrderByDescending(y => (y.Field<DateTime>("Posted")));
                }

                // last page posts
                var dataRows = rowList.Take(PageContext.BoardSettings.PostsPerPage);

                var altItem = false;

                // load the missing message test
                this.Get<YafDBBroker>().LoadMessageText(dataRows);

                string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);

                feed = new YafSyndicationFeed("{0}{1} - {2}".FormatWith(this.PageContext.Localization.GetText("PROFILE", "TOPIC"), this.PageContext.PageTopicName, PageContext.BoardSettings.PostsPerPage), feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

                foreach (var row in dataRows)
                {
                    DateTime posted = Convert.ToDateTime(row["Edited"]) + this.Get<YafDateTime>().TimeOffset;

                    if (syndicationItems.Count <= 0)
                    {
                        feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty, Convert.ToInt64(row["UserID"])));
                        feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;
                       
                    }
                 
                    List<SyndicationLink> attachementLinks = null;

                    // if the user doesn't have download access we simply don't show enclosure links.
                    if (PageContext.ForumDownloadAccess)
                    {
                        attachementLinks = GetMediaLinks(row["MessageID"].ToType<int>());
                    }

                    feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty, Convert.ToInt64(row["UserID"])));

                    syndicationItems.AddSyndicationItem(
                      row["Subject"].ToString(),
                      YafFormatMessage.FormatSyndicationMessage(row["Message"].ToString(), new MessageFlags(row["Flags"]), altItem, 4000),
                      null,
                      YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "m={0}#post{0}", row["MessageID"]),
                       "urn:{0}:ft{1}:st{2}:meid{3}:{4}".FormatWith(
                       urlAlphaNum, 
                       feedType,
                       atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                       row["MessageID"], PageContext.PageBoardID),
                       posted,
                       feed, 
                       attachementLinks);

                       // used to format feeds
                       altItem = !altItem;
                }

                feed.Items = syndicationItems;
            }
        }
       
        /// <summary>
        /// The method creates YafSyndicationFeed for forums in a category.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="categoryId">The category id.</param>
        private void GetForumFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, object categoryId)
        {
            var syndicationItems = new List<SyndicationItem>();
            using (
               DataTable dt = DB.forum_listread(
                 this.PageContext.PageBoardID, this.PageContext.PageUserID, categoryId, null))
            {
                string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);

                feed = new YafSyndicationFeed(this.PageContext.Localization.GetText("DEFAULT", "FORUM"), feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

                foreach (DataRow row in dt.Rows)
                {
                    if ((row["TopicMovedID"].IsNullOrEmptyDBField() && row["LastPosted"].IsNullOrEmptyDBField())) continue;
                    DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) + this.Get<YafDateTime>().TimeOffset;

                    if (syndicationItems.Count <= 0)
                    {
                        if (row["LastUserID"].IsNullOrEmptyDBField() || row["LastUserID"].IsNullOrEmptyDBField())
                        {
                            break;
                        }

                        feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(
                            String.Empty,
                            Convert.ToInt64(row["LastUserID"])));

                        feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;

                        // Alternate Link
                        // feed.Links.Add(new SyndicationLink(new Uri(YafBuildLink.GetLinkNotEscaped(ForumPages.topics, true))));
                    }

                    if (!row["LastUserID"].IsNullOrEmptyDBField())
                    {
                        feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(
                            String.Empty,
                            Convert.ToInt64(row["LastUserID"])));
                    }

                    syndicationItems.AddSyndicationItem(
                        row["Forum"].ToString(),
                        HtmlEncode(row["Description"].ToString()),
                        null,
                        YafBuildLink.GetLinkNotEscaped(ForumPages.topics, true, "f={0}", row["ForumID"]),
                        "urn:{0}:ft{1}:st{2}:fid{3}:lmid{4}:{5}".FormatWith(urlAlphaNum,
                        feedType, 
                        atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                        row["ForumID"], 
                        row["LastMessageID"], PageContext.PageBoardID),
                        lastPosted,
                        feed);
                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for topics in a forum.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="lastPostIcon">The icon for last post link.</param>
        /// <param name="lastPostName">The last post name.</param>
        /// <param name="forumId">The forum id.</param>
        private void GetTopicsFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, string lastPostIcon, string lastPostName, int forumId)
        {
            var syndicationItems = new List<SyndicationItem>();

            // vzrus changed to separate DLL specific code
            using (DataTable dt = DB.rsstopic_list(forumId))
            {
                string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);

                feed = new YafSyndicationFeed(this.PageContext.Localization.GetText("DEFAULT", "FORUM") + ":" + this.PageContext.PageForumName, feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

                foreach (DataRow row in dt.Rows)
                {

                    DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) +
                                          this.Get<YafDateTime>().TimeOffset;

                    if (syndicationItems.Count <= 0)
                    {

                        feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                        Convert.ToInt64(
                                                                                            row["LastUserID"])));
                        feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;

                        // Alternate Link
                        //  feed.Links.Add(new SyndicationLink(new Uri(YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true))));
                    }

                    feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                         Convert.ToInt64(
                                                                                             row["LastUserID"])));


                    syndicationItems.AddSyndicationItem(
                        row["Topic"].ToString(),
                        GetPostLatestContent(
                            YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "m={0}#post{0}",
                                                           row["LastMessageID"]), lastPostIcon, lastPostName,
                            lastPostName, String.Empty,
                            !row["LastMessageFlags"].IsNullOrEmptyDBField()
                                ? Convert.ToInt32(row["LastMessageFlags"])
                                : 22, false),
                        null,
                        YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "t={0}", row["TopicID"]),
                        "urn:{0}:ft{1}:st{2}:tid{3}:lmid{4}:{5}".FormatWith(urlAlphaNum, 
                       feedType,
                       atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                       row["TopicID"], row["LastMessageID"], PageContext.PageBoardID),
                       lastPosted,
                       feed);

                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for Active topics.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="lastPostIcon">The icon for last post link.</param>
        /// <param name="lastPostName">The last post name.</param>
        /// <param name="categoryActiveId"></param>
        private void GetActiveFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, string lastPostIcon, string lastPostName, object categoryActiveId)
        {
            var syndicationItems = new List<SyndicationItem>();
            DateTime toActDate = DateTime.UtcNow;
            string toActText = this.PageContext.Localization.GetText("MYTOPICS", "LAST_MONTH");

            if (this.Request.QueryString.GetFirstOrDefault("txt") != null)
            {
                toActText = Server.UrlDecode(Server.HtmlDecode(this.Request.QueryString.GetFirstOrDefault("txt").ToString()));
            }

            if (this.Request.QueryString.GetFirstOrDefault("d") != null)
            {
                if (!DateTime.TryParse(Server.UrlDecode(Server.HtmlDecode(this.Request.QueryString.GetFirstOrDefault("d").ToString())), out toActDate))
                {
                    toActDate = Convert.ToDateTime(this.Get<YafDateTime>().FormatDateTimeShort(DateTime.UtcNow)) + TimeSpan.FromDays(-31);
                    toActText = this.PageContext.Localization.GetText("MYTOPICS", "LAST_MONTH");
                }
                else
                {
                    // To limit number of feeds items by timespan if we are getting an unreasonable time                
                    if (toActDate < DateTime.UtcNow + TimeSpan.FromDays(-31))
                    {
                        toActDate = DateTime.UtcNow + TimeSpan.FromDays(-31);
                        toActText = this.PageContext.Localization.GetText("MYTOPICS", "LAST_MONTH");
                    }
                }
            }

            string urlAlphaNum =FormatUrlForFeed(BaseUrlBuilder.BaseUrl);
            string feedNameAlphaNum = new Regex(@"[^A-Za-z0-9]", RegexOptions.IgnoreCase).Replace(toActText.ToString(),
                                                                                                  String.Empty);
            feed = new YafSyndicationFeed(this.PageContext.Localization.GetText("MYTOPICS", "ACTIVETOPICS") + " - " + toActText, feedType, atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

            using (
              DataTable dt = DB.topic_active(
                this.PageContext.PageBoardID,
                this.PageContext.PageUserID,
                toActDate,
                categoryActiveId,
                false))
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row["TopicMovedID"].IsNullOrEmptyDBField())
                    {
                        DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) + this.Get<YafDateTime>().TimeOffset;

                        if (syndicationItems.Count <= 0)
                        {
                            feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                            Convert.ToInt64(row["UserID"])));
                            feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;
                           
                        }

                        feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                             Convert.ToInt64(
                                                                                                 row["LastUserID"])));

                        string messageLink = YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "m={0}#post{0}",
                                                                            row["LastMessageID"]);
                        syndicationItems.AddSyndicationItem(
                            row["Subject"].ToString(),
                            GetPostLatestContent(messageLink, lastPostIcon, lastPostName, lastPostName, String.Empty,
                                                 !row["LastMessageFlags"].IsNullOrEmptyDBField()
                                                     ? Convert.ToInt32(row["LastMessageFlags"])
                                                     : 22, false),
                            null,
                            messageLink,
                            "urn:{0}:ft{1}:st{2}:span{3}:ltid{4}:lmid{5}:{6}".FormatWith(urlAlphaNum,
                            feedNameAlphaNum,
                            feedType, 
                            atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                            row["LinkTopicID"],
                            row["LastMessageID"], PageContext.PageBoardID),
                            lastPosted, 
                            feed);
                    }
                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// The method creates YafSyndicationFeed for Favorite topics.
        /// </summary>
        /// <param name="feed">The YafSyndicationFeed.</param>
        /// <param name="feedType">The FeedType.</param>
        /// <param name="atomFeedByVar">The Atom feed checker.</param>
        /// <param name="lastPostIcon">The icon for last post link.</param>
        /// <param name="lastPostName">The last post name.</param>
        /// <param name="categoryActiveId"></param>
        private void GetFavoriteFeed(ref YafSyndicationFeed feed, YafRssFeeds feedType, bool atomFeedByVar, string lastPostIcon, string lastPostName, object categoryActiveId)
        {
            var syndicationItems = new List<SyndicationItem>();
            DateTime toFavDate = DateTime.UtcNow;
            string toFavText = this.PageContext.Localization.GetText("MYTOPICS", "LAST_MONTH");

            if (this.Request.QueryString.GetFirstOrDefault("txt") != null)
            {
                toFavText = Server.UrlDecode(Server.HtmlDecode(this.Request.QueryString.GetFirstOrDefault("txt").ToString()));
            }

            if (this.Request.QueryString.GetFirstOrDefault("d") != null)
            {
                if (!DateTime.TryParse(Server.UrlDecode(Server.HtmlDecode(this.Request.QueryString.GetFirstOrDefault("d").ToString())), out toFavDate))
                {
                    toFavDate = this.PageContext.CurrentUserData.Joined == null ? DateTime.MinValue + TimeSpan.FromDays(2) : (DateTime)this.PageContext.CurrentUserData.Joined;
                    toFavText = this.PageContext.Localization.GetText("MYTOPICS", "SHOW_ALL");
                }
            }
            else
            {
                toFavDate = this.PageContext.CurrentUserData.Joined == null ? DateTime.MinValue + TimeSpan.FromDays(2) : (DateTime)this.PageContext.CurrentUserData.Joined;
                toFavText = this.PageContext.Localization.GetText("MYTOPICS", "SHOW_ALL");
            }

            using (
              DataTable dt = DB.topic_favorite_details(
                this.PageContext.PageBoardID,
                this.PageContext.PageUserID,
                toFavDate,
                categoryActiveId,
                false))
            {
                string urlAlphaNum = FormatUrlForFeed(YafForumInfo.ForumBaseUrl);
                string feedNameAlphaNum = new Regex(@"[^A-Za-z0-9]", RegexOptions.IgnoreCase).Replace(toFavText.ToString(), String.Empty);
                feed =
                         new YafSyndicationFeed(
                             "{0} - {1}".FormatWith(this.PageContext.Localization.GetText("MYTOPICS", "FAVORITETOPICS"),
                                                    toFavText), feedType,
                             atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(), urlAlphaNum);

                foreach (DataRow row in dt.Rows)
                {
                    if (row["TopicMovedID"].IsNullOrEmptyDBField())
                    {

                        DateTime lastPosted = Convert.ToDateTime(row["LastPosted"]) + this.Get<YafDateTime>().TimeOffset;

                        if (syndicationItems.Count <= 0)
                        {
                            feed.Authors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                            Convert.ToInt64(row["UserID"])));
                            feed.LastUpdatedTime = DateTime.UtcNow + this.Get<YafDateTime>().TimeOffset;
                        
                        }

                        feed.Contributors.Add(SyndicationItemExtensions.NewSyndicationPerson(String.Empty,
                                                                                             Convert.ToInt64(
                                                                                                 row["LastUserID"])));

                        syndicationItems.AddSyndicationItem(
                            row["Subject"].ToString(),
                            GetPostLatestContent(
                                YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "m={0}#post{0}", row["LastMessageID"]),
                                lastPostIcon, lastPostName, lastPostName, String.Empty,
                                !row["LastMessageFlags"].IsNullOrEmptyDBField()
                                    ? Convert.ToInt32(row["LastMessageFlags"])
                                    : 22, false),
                            null,
                            YafBuildLink.GetLinkNotEscaped(ForumPages.posts, true, "t={0}", row["LinkTopicID"]),
                           "urn:{0}:ft{1}:st{2}:span{3}:ltid{4}:lmid{5}:{6}".FormatWith(urlAlphaNum,
                           feedType, 
                           atomFeedByVar ? YafSyndicationFormats.Atom.ToInt() : YafSyndicationFormats.Rss.ToInt(),
                           feedNameAlphaNum, 
                           Convert.ToInt32(row["LinkTopicID"]),
                           row["LastMessageID"], PageContext.PageBoardID),
                           lastPosted,
                           feed);
                    }
                }

                feed.Items = syndicationItems;
            }
        }

        /// <summary>
        /// Format the Url to an URN compatible string
        /// </summary>
        /// <param name="InputUrl">Input Url to format</param>
        /// <returns>Formatted url</returns>
        private static string FormatUrlForFeed(string InputUrl)
        {
            string formatedUrl = InputUrl;

            if (formatedUrl.Contains(@"http://www."))
            {
                formatedUrl = formatedUrl.Replace("http://www.", String.Empty);
            }
            else if (formatedUrl.Contains(@"http://"))
            {
                formatedUrl = formatedUrl.Replace("http://", String.Empty);
            }

            formatedUrl = formatedUrl.Replace(".", "-").Replace("/", "-");

            if (formatedUrl.EndsWith("/"))
            {
                formatedUrl = formatedUrl.Remove(formatedUrl.Length - 1);
            }

            return formatedUrl;
        }

        /// <summary>
        /// The helper function gets media enclosure links for a post
        /// </summary>
        /// <param name="messageId">The MessageId with attached files.</param>
        /// <returns></returns>
        private List<SyndicationLink> GetMediaLinks(int messageId)
        {
            var attachementLinks = new List<SyndicationLink>();
            using (var attList = DB.attachment_list(messageId, null, PageContext.PageBoardID))
            {
                if (attList.Rows.Count > 0)
                {
                    foreach (DataRow attachLink in attList.Rows)
                    {
                        if (!attachLink["FileName"].IsNullOrEmptyDBField())
                        {
                            attachementLinks.Add(new SyndicationLink(
                                    new Uri("{0}{1}resource.ashx?a={2}".FormatWith(
                                        YafForumInfo.ForumBaseUrl,
                                        YafForumInfo.ForumClientFileRoot.TrimStart('/'), attachLink["AttachmentID"])), "enclosure", attachLink["FileName"].ToString(), attachLink["ContentType"].ToString(), attachLink["Bytes"].ToType<long>()));

                        }

                    }

                }

            }
            return attachementLinks;
        }

        #endregion
    }
}