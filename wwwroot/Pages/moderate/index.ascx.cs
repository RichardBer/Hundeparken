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

namespace YAF.Pages.moderate
{
  using System;
  using System.Data;
  using System.Web.UI.WebControls;
  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Utils;

  /// <summary>
  /// Base root control for moderating, linking to other moderating controls/pages.
  /// </summary>
  public partial class index : ForumPage
  {
    #region Construcotrs & Overridden Methods

    /// <summary>
    /// Initializes a new instance of the <see cref="index"/> class. 
    /// Default constructor.
    /// </summary>
    public index()
      : base("MODERATE_DEFAULT")
    {
    }


    /// <summary>
    /// Creates page links for this page.
    /// </summary>
    protected override void CreatePageLinks()
    {
      // forum index
      this.PageLinks.AddLink(PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));

      // moderation index
      this.PageLinks.AddLink(GetText("TITLE"));
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles page load event.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Page_Load(object sender, EventArgs e)
    {
      // Only moderators are allowed here
      if (!PageContext.IsModerator)
      {
        YafBuildLink.AccessDenied();
      }

      // this needs to be done just once, not during postbacks
      if (!IsPostBack)
      {
        // create page links
        CreatePageLinks();

        // bind data
        BindData();
      }
    }


    /// <summary>
    /// Handles event of item commands for each forum.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ForumList_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
      // which command are we handling
      switch (e.CommandName.ToLower())
      {
        case "viewunapprovedposts":

          // go to unapproved posts for selected forum
          YafBuildLink.Redirect(ForumPages.moderate_unapprovedposts, "f={0}", e.CommandArgument);
          break;        
        case "viewreportedposts":

          // go to spam reports for selected forum
          YafBuildLink.Redirect(ForumPages.moderate_reportedposts, "f={0}", e.CommandArgument);
          break;
      }
    }

    #endregion

    #region Data Binding & Formatting

    /// <summary>
    /// Bind data for this control.
    /// </summary>
    private void BindData()
    {
      // get list of forums and their moderating data
      using (DataSet ds = DB.forum_moderatelist(PageContext.PageUserID, PageContext.PageBoardID))
      {
        this.CategoryList.DataSource = ds.Tables[YafDBAccess.GetObjectName("Category")];
      }

      // bind data to controls
      DataBind();
    }

    #endregion
  }
}