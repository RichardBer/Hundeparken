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

using HPSagaYafCustomization;

namespace YAF.Pages
{
  // YAF.Pages
  #region Using

  using System;
  using System.Data;
  using System.Web.Security;
  using System.Web.UI;
  using System.Web.UI.WebControls;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Pattern;
  using YAF.Classes.Utils;
  using YAF.Controls;

  #endregion

  /// <summary>
  /// Summary description for profile.
  /// </summary>
  public partial class profile : ForumPage
  {
    #region Constants and Fields

    /// <summary>
    ///   The forum access.
    /// </summary>
    protected Repeater ForumAccess;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    ///   Initializes a new instance of the <see cref = "profile" /> class.
    /// </summary>
    public profile()
      : base("PROFILE")
    {
      this.AllowAsPopup = true;
      CustomProfile.CheckForProfileRedirect(YafContext.HttpContext);
    }

    #endregion

    #region Properties

    /// <summary>
    ///   Gets or sets UserId.
    /// </summary>
    public int UserId
    {
      get
      {
        if (this.ViewState["UserId"] == null)
        {
          return 0;
        }

        return (int)this.ViewState["UserId"];
      }

      set
      {
        this.ViewState["UserId"] = value;
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// The albums tab is visible.
    /// </summary>
    /// <returns>
    /// The true if albums tab should be visible.
    /// </returns>
    protected bool AlbumsTabIsVisible()
    {
      int albumUser = this.PageContext.PageUserID;

      if (this.PageContext.IsAdmin && this.UserId > 0)
      {
        albumUser = this.UserId;
      }

      // Add check if Albums Tab is visible 
      if (!this.PageContext.IsGuest && YafContext.Current.BoardSettings.EnableAlbum)
      {
        int albumCount = DB.album_getstats(albumUser, null)[0];

        // Check if the user already has albums.
        if (albumCount > 0)
        {
          return true;
        }

        // If this is the album owner we show him the tab, else it should be hidden 
        if ((albumUser == this.PageContext.PageUserID) || this.PageContext.IsAdmin)
        {
          // Check if a user have permissions to have albums, even if he has no albums at all.
          var usrAlbums =
            DB.user_getalbumsdata(albumUser, YafContext.Current.PageBoardID).GetFirstRowColumnAsValue<int?>(
              "UsrAlbums", null);

          if (usrAlbums.HasValue && usrAlbums > 0)
          {
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// The collapsible image_ on click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void CollapsibleImage_OnClick([NotNull] object sender, [NotNull] ImageClickEventArgs e)
    {
      this.BindData();
    }

    /// <summary>
    /// The page_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
    {
      this.lnkThanks.Text = "({0})".FormatWith(this.PageContext.Localization.GetText("VIEWTHANKS", "TITLE"));
      this.lnkThanks.Visible = YafContext.Current.BoardSettings.EnableThanksMod;

      // admin or moderator, set edit control to moderator mode...
      if (this.PageContext.IsAdmin || this.PageContext.IsForumModerator)
      {
        this.SignatureEditControl.InModeratorMode = true;
      }

      if (!this.IsPostBack)
      {
        this.UserId = (int)Security.StringToLongOrRedirect(this.Request.QueryString.GetFirstOrDefault("u"));
        this.userGroupsRow.Visible = this.PageContext.BoardSettings.ShowGroupsProfile || this.PageContext.IsAdmin;
      }

      if (this.UserId == 0)
      {
        YafBuildLink.AccessDenied( /*No such user exists*/);
      }

      this.AlbumListTab.Visible = this.AlbumsTabIsVisible();
      this.BindData();
    }

    /// <summary>
    /// The setup theme button with link.
    /// </summary>
    /// <param name="thisButton">
    /// The this button.
    /// </param>
    /// <param name="linkUrl">
    /// The link url.
    /// </param>
    protected void SetupThemeButtonWithLink([NotNull] ThemeButton thisButton, [NotNull] string linkUrl)
    {
      if (linkUrl.IsSet())
      {
        string link = linkUrl.Replace("\"", string.Empty);
        if (!link.ToLower().StartsWith("http"))
        {
          link = "http://" + link;
        }

        thisButton.NavigateUrl = link;
        thisButton.Attributes.Add("target", "_blank");
        if (this.PageContext.BoardSettings.UseNoFollowLinks)
        {
          thisButton.Attributes.Add("rel", "nofollow");
        }
      }
      else
      {
        thisButton.NavigateUrl = string.Empty;
      }
    }

    /// <summary>
    /// The lnk_ add buddy.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void lnk_AddBuddy([NotNull] object sender, [NotNull] CommandEventArgs e)
    {
      if (e.CommandArgument.ToString() == "addbuddy")
      {
        string[] strBuddyRequest = YafBuddies.AddBuddyRequest(this.UserId);

        var linkButton = (LinkButton)this.AboutTab.FindControl("lnkBuddy");

        linkButton.Visible = false;

        if (Convert.ToBoolean(strBuddyRequest[1]))
        {
          this.PageContext.AddLoadMessage(
            this.PageContext.Localization.GetText("NOTIFICATION_BUDDYAPPROVED_MUTUAL").FormatWith(strBuddyRequest[0]));
        }
        else
        {
          var literal = (Literal)this.AboutTab.FindControl("ltrApproval");
          literal.Visible = true;
          this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("NOTIFICATION_BUDDYREQUEST"));
        }
      }
      else
      {
        this.PageContext.AddLoadMessage(
          this.PageContext.Localization.GetText("REMOVEBUDDY_NOTIFICATION").FormatWith(
            YafBuddies.RemoveBuddy(this.UserId)));
      }

      this.BindData();
    }

    /// <summary>
    /// The lnk_ view thanks.
    /// </summary>
    /// <param name="sender">
    /// the sender.
    /// </param>
    /// <param name="e">
    /// the e.
    /// </param>
    protected void lnk_ViewThanks([NotNull] object sender, [NotNull] CommandEventArgs e)
    {
      YafBuildLink.Redirect(ForumPages.viewthanks, "u={0}", this.UserId);
    }

    /// <summary>
    /// The add page links.
    /// </summary>
    /// <param name="userDisplayName">
    /// The user display name.
    /// </param>
    private void AddPageLinks([NotNull] string userDisplayName)
    {
      this.PageLinks.Clear();
      this.PageLinks.AddLink(this.PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));
      this.PageLinks.AddLink(
        this.GetText("MEMBERS"), 
        this.Get<YafPermissions>().Check(this.PageContext.BoardSettings.MembersListViewPermissions)
          ? YafBuildLink.GetLink(ForumPages.members)
          : null);
      this.PageLinks.AddLink(userDisplayName, string.Empty);
    }

    /// <summary>
    /// The bind data.
    /// </summary>
    private void BindData()
    {
      MembershipUser user = UserMembershipHelper.GetMembershipUserById(this.UserId);

      if (user == null || user.ProviderUserKey.ToString() == "0")
      {
        YafBuildLink.AccessDenied( /*No such user exists or this is an nntp user ("0") */);
      }

      var userData = new CombinedUserDataHelper(user, this.UserId);

      // populate user information controls...      
      // Is BuddyList feature enabled?
      if (YafContext.Current.BoardSettings.EnableBuddyList)
      {
        this.SetupBuddyList(this.UserId, userData);
      }
      else
      {
        // BuddyList feature is disabled. don't show any link.
        this.lnkBuddy.Visible = false;
        this.ltrApproval.Visible = false;
      }

      // Is album feature enabled?
      if (YafContext.Current.BoardSettings.EnableAlbum)
      {
        this.AlbumList1.UserID = this.UserId;
      }
      else
      {
        this.AlbumList1.Dispose();
      }

      string userDisplayName = this.PageContext.UserDisplayName.GetName(this.UserId);

      this.SetupUserProfileInfo(this.UserId, user, userData, userDisplayName);

      this.AddPageLinks(userDisplayName);

      this.SetupUserStatistics(userData);

      // private messages
      this.SetupUserLinks(userData);

      // localize tab titles...
      this.LocalizeTabTitles(this.UserId);

      this.SetupAvatar(this.UserId, userData);

      this.Groups.DataSource = RoleMembershipHelper.GetRolesForUser(UserMembershipHelper.GetUserNameFromID(this.UserId));

      // EmailRow.Visible = PageContext.IsAdmin;
      this.ProfileTabs.Views["ModerateTab"].Visible = this.PageContext.IsAdmin || this.PageContext.IsForumModerator;
      this.ProfileTabs.Views["ModerateTab"].Text = this.GetText("MODERATION");
      this.AdminUserButton.Visible = this.PageContext.IsAdmin;

      if (this.LastPosts.Visible)
      {
        this.LastPosts.DataSource =
          DB.post_alluser(this.PageContext.PageBoardID, this.UserId, this.PageContext.PageUserID, 10).AsEnumerable();
        this.SearchUser.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.search, "postedby={0}", userDisplayName);
      }

      this.DataBind();
    }

    /// <summary>
    /// The localize tab titles.
    /// </summary>
    /// ///
    /// <param name="userID">
    /// The user id.
    /// </param>
    private void LocalizeTabTitles(int userID)
    {
      this.ProfileTabs.Views["AboutTab"].Text = this.GetText("ABOUT");
      this.ProfileTabs.Views["StatisticsTab"].Text = this.GetText("STATISTICS");
      this.ProfileTabs.Views["AvatarTab"].Text = this.GetText("AVATAR");
      this.ProfileTabs.Views["Last10PostsTab"].Text = this.GetText("LAST10");

      this.ProfileTabs.Views["BuddyListTab"].Text =
        this.GetText(userID == this.PageContext.PageUserID ? "BUDDIES" : "BUDDIESTITLE");

      this.ProfileTabs.Views["AlbumListTab"].Text = this.GetText("ALBUMS");
    }

    /// <summary>
    /// The setup avatar.
    /// </summary>
    /// <param name="userID">
    /// The user id.
    /// </param>
    /// <param name="userData">
    /// The user data.
    /// </param>
    private void SetupAvatar(int userID, [NotNull] CombinedUserDataHelper userData)
    {
      if (this.PageContext.BoardSettings.AvatarUpload && userData.HasAvatarImage)
      {
        this.Avatar.ImageUrl = YafForumInfo.ForumClientFileRoot + "resource.ashx?u=" + userID;
      }
      else if (userData.Avatar.IsSet())
      {
        // Took out PageContext.BoardSettings.AvatarRemote
        this.Avatar.ImageUrl =
          "{3}resource.ashx?url={0}&width={1}&height={2}".FormatWith(
            this.Server.UrlEncode(userData.Avatar), 
            this.PageContext.BoardSettings.AvatarWidth, 
            this.PageContext.BoardSettings.AvatarHeight, 
            YafForumInfo.ForumClientFileRoot);
      }
      else
      {
        this.Avatar.Visible = false;
        this.AvatarTab.Visible = false;
      }
    }

    /// <summary>
    /// The setup buddy list.
    /// </summary>
    /// <param name="userID">
    /// The user id.
    /// </param>
    /// <param name="userData">
    /// The user data.
    /// </param>
    private void SetupBuddyList(int userID, [NotNull] CombinedUserDataHelper userData)
    {
      if (userID == this.PageContext.PageUserID)
      {
        this.lnkBuddy.Visible = false;
      }
      else if (YafBuddies.IsBuddy((int)userData.DBRow["userID"], true) && !this.PageContext.IsGuest)
      {
        this.lnkBuddy.Visible = true;
        this.lnkBuddy.Text = "({0})".FormatWith(this.PageContext.Localization.GetText("BUDDY", "REMOVEBUDDY"));
        this.lnkBuddy.CommandArgument = "removebuddy";
        this.lnkBuddy.Attributes["onclick"] = String.Format(
          "return confirm('{0}')", this.PageContext.Localization.GetText("CP_EDITBUDDIES", "NOTIFICATION_REMOVE"));
      }
      else if (YafBuddies.IsBuddy((int)userData.DBRow["userID"], false))
      {
        this.lnkBuddy.Visible = false;
        this.ltrApproval.Visible = true;
      }
      else
      {
        if (!this.PageContext.IsGuest)
        {
          this.lnkBuddy.Visible = true;
          this.lnkBuddy.Text = "({0})".FormatWith(this.PageContext.Localization.GetText("BUDDY", "ADDBUDDY"));
          this.lnkBuddy.CommandArgument = "addbuddy";
          this.lnkBuddy.Attributes["onclick"] = string.Empty;
        }
      }

      this.BuddyList.CurrentUserID = userID;
      this.BuddyList.Mode = 1;
    }

    /// <summary>
    /// The setup user links.
    /// </summary>
    /// <param name="userData">
    /// The user data.
    /// </param>
    private void SetupUserLinks([NotNull] CombinedUserDataHelper userData)
    {
      // homepage link
      this.Home.Visible = userData.Profile.Homepage.IsSet();
      this.SetupThemeButtonWithLink(this.Home, userData.Profile.Homepage);

      // blog link
      this.Blog.Visible = userData.Profile.Blog.IsSet();
      this.SetupThemeButtonWithLink(this.Blog, userData.Profile.Blog);

      if (userData.UserID != this.PageContext.PageUserID)
      {
        this.PM.Visible = !userData.IsGuest && this.User != null && this.PageContext.BoardSettings.AllowPrivateMessages;
        this.PM.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.pmessage, "u={0}", userData.UserID);

        // email link
        this.Email.Visible = !userData.IsGuest && this.User != null && this.PageContext.BoardSettings.AllowEmailSending;
        this.Email.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_email, "u={0}", userData.UserID);
        if (this.PageContext.IsAdmin)
        {
          this.Email.TitleNonLocalized = userData.Membership.Email;
        }

        this.MSN.Visible = this.User != null && userData.Profile.MSN.IsSet();
        this.MSN.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_msn, "u={0}", userData.UserID);

        this.YIM.Visible = this.User != null && userData.Profile.YIM.IsSet();
        this.YIM.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_yim, "u={0}", userData.UserID);

        this.AIM.Visible = this.User != null && userData.Profile.AIM.IsSet();
        this.AIM.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_aim, "u={0}", userData.UserID);

        this.ICQ.Visible = this.User != null && userData.Profile.ICQ.IsSet();
        this.ICQ.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_icq, "u={0}", userData.UserID);

        this.XMPP.Visible = this.User != null && userData.Profile.XMPP.IsSet();
        this.XMPP.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_xmpp, "u={0}", userData.UserID);

        this.Skype.Visible = this.User != null && userData.Profile.Skype.IsSet();
        this.Skype.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.im_skype, "u={0}", userData.UserID);
      }
    }

    /// <summary>
    /// The setup user profile info.
    /// </summary>
    /// <param name="userID">
    /// The user id.
    /// </param>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="userData">
    /// The user data.
    /// </param>
    /// <param name="userDisplayName">
    /// The user display name.
    /// </param>
    private void SetupUserProfileInfo(
      int userID, [NotNull] MembershipUser user, [NotNull] CombinedUserDataHelper userData, [NotNull] string userDisplayName)
    {
      this.UserLabel1.UserID = userData.UserID;

      if (this.PageContext.IsAdmin && userDisplayName != user.UserName)
      {
        this.Name.Text = this.HtmlEncode("{0} ({1})".FormatWith(userDisplayName, user.UserName));
      }
      else
      {
        this.Name.Text = this.HtmlEncode(userDisplayName);
      }

      this.Joined.Text = "{0}".FormatWith(this.Get<YafDateTime>().FormatDateLong(Convert.ToDateTime(userData.Joined)));

      // vzrus: Show last visit only to admins if user is hidden
      if (!this.PageContext.IsAdmin && Convert.ToBoolean(userData.DBRow["IsActiveExcluded"]))
      {
        this.LastVisit.Text = this.GetText("COMMON", "HIDDEN");
        this.LastVisit.Visible = true;
      }
      else
      {
        this.LastVisitDateTime.DateTime = userData.LastVisit;
        this.LastVisitDateTime.Visible = true;
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.RankName))
      {
        this.RankTR.Visible = true;
        this.Rank.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.RankName));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.Location))
      {
        this.LocationTR.Visible = true;
        this.Location.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.Location));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.Location))
      {
        this.LocationTR.Visible = true;
        this.Location.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.Location));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.RealName))
      {
        this.RealNameTR.Visible = true;
        this.RealName.InnerHtml = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.RealName));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.Interests))
      {
        this.InterestsTR.Visible = true;
        this.Interests.InnerHtml = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.Interests));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.Occupation))
      {
        this.OccupationTR.Visible = true;
        this.Occupation.InnerHtml = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.Occupation));
      }

      // Handled in localization. 
      this.Gender.InnerText = this.GetText("GENDER" + userData.Profile.Gender);

      this.ThanksFrom.Text = DB.user_getthanks_from(userData.DBRow["userID"], this.PageContext.PageUserID).ToString();
      int[] thanksToArray = DB.user_getthanks_to(userData.DBRow["userID"], this.PageContext.PageUserID);
      this.ThanksToTimes.Text = thanksToArray[0].ToString();
      this.ThanksToPosts.Text = thanksToArray[1].ToString();
      this.OnlineStatusImage1.UserID = userID;
      this.OnlineStatusImage1.Visible = this.PageContext.BoardSettings.ShowUserOnlineStatus;

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.XMPP))
      {
        this.XmppTR.Visible = true;
        this.lblxmpp.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.XMPP));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.AIM))
      {
        this.AimTR.Visible = true;
        this.lblaim.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.AIM));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.ICQ))
      {
        this.IcqTR.Visible = true;
        this.lblicq.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.ICQ));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.MSN))
      {
        this.MsnTR.Visible = true;
        this.lblmsn.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.MSN));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.Skype))
      {
        this.SkypeTR.Visible = true;
        this.lblskype.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.Skype));
      }

      if (this.User != null && !string.IsNullOrEmpty(userData.Profile.YIM))
      {
        this.YimTR.Visible = true;
        this.lblyim.Text = this.HtmlEncode(this.Get<YafBadWordReplace>().Replace(userData.Profile.YIM));
      }

      if (this.User != null && userData.Profile.Birthday != DateTime.MinValue)
      {
        this.BirthdayTR.Visible = true;
        this.Birthday.Text =
          this.Get<YafDateTime>().FormatDateLong(
                  userData.Profile.Birthday.Date);
          // .Add(-this.Get<YafDateTime>().TimeOffset));
      }
      else
      {
        this.BirthdayTR.Visible = false;
      }
    }

    /// <summary>
    /// The setup user statistics.
    /// </summary>
    /// <param name="userData">
    /// The user data.
    /// </param>
    private void SetupUserStatistics([NotNull] CombinedUserDataHelper userData)
    {
      double allPosts = 0.0;

      if (SqlDataLayerConverter.VerifyInt32(userData.DBRow["NumPostsForum"]) > 0)
      {
        allPosts = 100.0 * SqlDataLayerConverter.VerifyInt32(userData.DBRow["NumPosts"]) /
                   SqlDataLayerConverter.VerifyInt32(userData.DBRow["NumPostsForum"]);
      }

      this.Stats.InnerHtml = "{0:N0}<br />[{1} / {2}]".FormatWith(
        userData.DBRow["NumPosts"], 
        this.GetTextFormatted("NUMALL", allPosts), 
        this.GetTextFormatted(
          "NUMDAY", 
          (double)SqlDataLayerConverter.VerifyInt32(userData.DBRow["NumPosts"]) /
          SqlDataLayerConverter.VerifyInt32(userData.DBRow["NumDays"])));
    }

    #endregion
  }
}