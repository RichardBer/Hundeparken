/* YetAnotherForum.NET
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

namespace YAF.Pages
{
  #region Using

  using System;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Utils;
  using YAF.Controls;

  #endregion

  /// <summary>
  /// The cp_pm.
  /// </summary>
  public partial class cp_pm : ForumPageRegistered
  {
    #region Constants and Fields

    /// <summary>
    /// The _view.
    /// </summary>
    private PMView _view;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="cp_pm"/> class.
    /// </summary>
    public cp_pm()
      : base("CP_PM")
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets View.
    /// </summary>
    protected PMView View
    {
      get
      {
        return this._view;
      }
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
      // check if this feature is disabled
      if (!this.PageContext.BoardSettings.AllowPrivateMessages)
      {
        YafBuildLink.RedirectInfoPage(InfoMessage.Disabled);
      }

      if (!this.IsPostBack)
      {
        this._view = PMViewConverter.FromQueryString(this.Request.QueryString.GetFirstOrDefault("v"));

        this.PmTabs.SelectedIndex = (int)this._view;

        // if (_view == PMView.Inbox)
        // this.PMTabs.ActiveTab = this.InboxTab;
        // else if (_view == PMView.Outbox)
        // this.PMTabs.ActiveTab = this.OutboxTab;
        // else if (_view == PMView.Archive)
        // this.PMTabs.ActiveTab = this.ArchiveTab;
        this.PageLinks.AddLink(this.PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));
        this.PageLinks.AddLink(this.PageContext.PageUserName, YafBuildLink.GetLink(ForumPages.cp_profile));
        this.PageLinks.AddLink(this.GetText("TITLE"));

        this.PmTabs.Views[0].Text = this.GetText("INBOX");
        this.PmTabs.Views[1].Text = this.GetText("SENTITEMS");
        this.PmTabs.Views[2].Text = this.GetText("ARCHIVE");

        // InboxTab.HeaderText = GetText("INBOX");
        // OutboxTab.HeaderText = GetText("SENTITEMS");
        // ArchiveTab.HeaderText = GetText("ARCHIVE");
        this.NewPM.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.pmessage);
        this.NewPM2.NavigateUrl = this.NewPM.NavigateUrl;

        // inbox tab
        // ScriptManager.RegisterClientScriptBlock(InboxTabUpdatePanel, typeof(UpdatePanel), "InboxTabRefresh", String.Format("function InboxTabRefresh() {1}\n__doPostBack('{0}', '');\n{2}", InboxTabUpdatePanel.ClientID, '{', '}'), true);
        // sent tab
        // ScriptManager.RegisterClientScriptBlock(SentTabUpdatePanel, typeof(UpdatePanel), "SentTabRefresh", String.Format("function SentTabRefresh() {1}\n__doPostBack('{0}', '');\n{2}", SentTabUpdatePanel.ClientID, '{', '}'), true);
        // archive tab
        // ScriptManager.RegisterClientScriptBlock(ArchiveTabUpdatePanel, typeof(UpdatePanel), "ArchiveTabRefresh", String.Format("function ArchiveTabRefresh() {1}\n__doPostBack('{0}', '');\n{2}", ArchiveTabUpdatePanel.ClientID, '{', '}'), true);
      }
    }

    #endregion
  }
}