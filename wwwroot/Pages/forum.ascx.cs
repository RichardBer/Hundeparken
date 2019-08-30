/* Yet Another Forum.NET
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
using System.Web;

namespace YAF.Pages
{
  #region Using

  using System;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Utils;

  #endregion

  /// <summary>
  /// Summary description for _default.
  /// </summary>
  public partial class forum : ForumPage
  {
    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="forum"/> class.
    /// </summary>
    public forum()
      : base("DEFAULT")
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
       this.PollList.Visible = this.PageContext.BoardSettings.BoardPollID > 0;
       this.PollList.PollGroupId = this.PageContext.BoardSettings.BoardPollID;
       this.PollList.BoardId = this.PageContext.Settings.BoardID;

      if (!this.IsPostBack)
      {

          // vzrus: needs testing, potentially can cause problems 
          if (!(UserAgentHelper.IsSearchEngineSpider(HttpContext.Current.Request.UserAgent)))
          {
              if (!HttpContext.Current.Request.Browser.Cookies)
              {
                  YafBuildLink.RedirectInfoPage(InfoMessage.RequiresCookies);
              }

              Version ecmaVersion = HttpContext.Current.Request.Browser.EcmaScriptVersion;

              if (ecmaVersion != null)
              {
                  if (!(ecmaVersion.Major > 0))
                  {
                      YafBuildLink.RedirectInfoPage(InfoMessage.EcmaScriptVersionUnsupported);
                  }
              }
              else
              {
                  YafBuildLink.RedirectInfoPage(InfoMessage.RequiresEcmaScript);
              }
          }

        this.ShoutBox1.Visible = this.PageContext.BoardSettings.ShowShoutbox;
        this.ForumStats.Visible = this.PageContext.BoardSettings.ShowForumStatistics;
        this.ActiveDiscussions.Visible = this.PageContext.BoardSettings.ShowActiveDiscussions;
        
       
          if (this.PageContext.Settings.LockedForum == 0)
        {
          this.PageLinks.AddLink(this.PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));
          if (this.PageContext.PageCategoryID != 0)
          {
            this.PageLinks.AddLink(
              this.PageContext.PageCategoryName, 
              YafBuildLink.GetLink(ForumPages.forum, "c={0}", this.PageContext.PageCategoryID));
            this.Welcome.Visible = false;
          }
        }
      }
    }

    #endregion
  }
}