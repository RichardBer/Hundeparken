﻿/* Yet Another Forum.net
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
using System.Collections;

namespace YAF.Pages
{
  #region Using

  using System;
  using System.Collections.Generic;
  using System.Data;
  using System.Drawing;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Web;
  using System.Web.UI.WebControls;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Utils;

  #endregion

  /// <summary>
  /// The polledit.
  /// </summary>
  public partial class polledit : ForumPage
  {
    #region Constants and Fields

    /// <summary>
    ///   Table with choices
    /// </summary>
    private DataRow _topicInfo;

    /// <summary>
    /// The board id.
    /// </summary>
    private int? _boardId;

    /// <summary>
    /// The category id.
    /// </summary>
    private int? _categoryId;

    /// <summary>
    ///   Table with choices
    /// </summary>
    private DataTable _choices;

    /// <summary>
    /// The date poll expire.
    /// </summary>
    private DateTime? _datePollExpire;

    /// <summary>
    /// The days poll expire.
    /// </summary>
    private int _daysPollExpire;

    /// <summary>
    /// The edit board id.
    /// </summary>
    private int? _editBoardId;

    /// <summary>
    /// The edit category id.
    /// </summary>
    private int? _editCategoryId;

    /// <summary>
    /// The edit forum id.
    /// </summary>
    private int? editForumId;

    /// <summary>
    /// The edit topic id.
    /// </summary>
    private int? _editTopicId;

    /// <summary>
    /// The edit message id.
    /// </summary>
    private int? _editMessageId;

    /// <summary>
    /// The forum id.
    /// </summary>
    private int? _forumId;

    /// <summary>
    /// The return forum.
    /// </summary>
    private int? _returnForum;

    /// <summary>
    /// The topic id.
    /// </summary>
    private int? _topicId;

    /// <summary>
    /// The topic unapproved.
    /// </summary>
    private bool _topicUnapproved;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="polledit"/> class. 
    ///   Initializes a new instance of the ReportPost class.
    /// </summary>
    public polledit()
      : base("POLLEDIT")
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets PollID.
    /// </summary>
    protected int? PollId { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// An image reader to read images on local disk.
    /// </summary>
    /// <param name="path">
    /// The path.
    /// </param>
    public Stream GetLocalData(Uri path)
    {
      return new FileStream(path.LocalPath, FileMode.Open);
    }

      /// <summary>
      /// The get remote data.
      /// </summary>
      /// <param name="url">
      /// The url.
      /// </param>
      /// <param name="length">
      /// The content length in bits.
      /// </param>
      /// <param name="contentType">
      /// The content type.
      /// </param>
      /// <returns>
      /// the Stream class.
      /// </returns>
      public Stream GetRemoteData(Uri url, out long length, out string contentType)
    {
      string path = url.ToString();
      length = 0;
      contentType = String.Empty;
      try
      {
        if (path.StartsWith("~/"))
        {
          path = "file://" + HttpRuntime.AppDomainAppPath + path.Substring(2, path.Length - 2);
        }

        WebRequest request = WebRequest.Create(new Uri(path));

        WebResponse response = request.GetResponse();
        length =  response.ContentLength;
        contentType =  response.ContentType;
        return response.GetResponseStream();
      }
      catch
      {
        return new MemoryStream();
      }
 // Don't make the program crash just because we have a picture which failed downloading
    }

    #endregion

    #region Methods

    /// <summary>
    /// The cancel_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="eventArgs">
    /// The event args.
    /// </param>
    protected void Cancel_Click(object sender, EventArgs eventArgs)
    {
      this.ReturnToPage();
    }

    /// <summary>
    /// From a path, return a byte[] of the image.
    /// </summary>
    /// <param name="uriPath">
    /// External image path.
    /// </param>
    /// <param name="length">
    /// The image size in bytes.
    /// </param>
    /// <returns>
    /// The get image parameters.
    /// </returns>
    protected string GetImageParameters(Uri uriPath, out long length)
    {
      string pseudoMime = string.Empty;
      string contentType = string.Empty;
      using (Stream stream = this.GetRemoteData(uriPath, out length, out contentType))
      {
        Bitmap img = null;
        try
        {
          img = new Bitmap(stream);

          // no need to set here mime exatly this is reserved for customization.
          pseudoMime = "{0}!{1};{2}".FormatWith(contentType, img.Width, img.Height);
        }
        catch
        {
            return String.Empty;
        }
        finally
        {
          if (img != null)
          {
            img.Dispose();
          }
        }
        stream.Close();
      }

        return pseudoMime;
    }

    /// <summary>
    /// The is input verified.
    /// </summary>
    /// <returns>
    /// The is input verified.
    /// </returns>
    protected bool IsInputVerified()
    {
      if (Convert.ToInt32(this.PollGroupListDropDown.SelectedIndex) <= 0)
      {
          if (this.Question.Text.Trim().Length == 0)
          {
              YafContext.Current.AddLoadMessage(YafContext.Current.Localization.GetText("POLLEDIT", "NEED_QUESTION"));
              return false;
          }

          // If it's admin or moderator we don't check tags
          if (!PageContext.IsAdmin || PageContext.IsForumModerator)
          {
              string tagPoll = YafFormatMessage.CheckHtmlTags(this.Question.Text.Trim(),
                                                              PageContext.BoardSettings.AcceptedHeadersHTML, ',');

              if (tagPoll.IsSet())
              {
                  this.PageContext.AddLoadMessage(tagPoll);
                  return false;
              }
          }


          int notNullcount = 0;
          foreach (RepeaterItem ri in this.ChoiceRepeater.Items)
          {
              string value = ((TextBox) ri.FindControl("PollChoice")).Text.Trim();

              if (!string.IsNullOrEmpty(value))
              {
                  notNullcount++;

                  // If it's admin or moderator we don't check tags
                  if (!PageContext.IsAdmin || PageContext.IsForumModerator)
                  {
                      string tagChoice = YafFormatMessage.CheckHtmlTags(value,
                                                                        PageContext.BoardSettings.AcceptedHeadersHTML,
                                                                        ',');
                      if (tagChoice.IsSet())
                      {
                          this.PageContext.AddLoadMessage(tagChoice);
                          return false;
                      }
                  }
              }
          }

          if (notNullcount < 2)
          {
              YafContext.Current.AddLoadMessage(YafContext.Current.Localization.GetText("POLLEDIT", "NEED_CHOICES"));
              return false;
          }

         
          int dateVerified = 0;
          if (!int.TryParse(this.PollExpire.Text.Trim(), out dateVerified) &&
              (this.PollExpire.Text.Trim().IsSet()))
          {
              YafContext.Current.AddLoadMessage(YafContext.Current.Localization.GetText("POLLEDIT", "EXPIRE_BAD"));
              return false;
          }

          // Set default value
          if (this.PollExpire.Text.Trim().IsNotSet() && this.IsClosedBoundCheckBox.Checked)
          {
              this.PollExpire.Text = "1";
          }
      }

        return true;
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
    protected void Page_Load(object sender, EventArgs e)
    {
      this.PollExpire.Attributes.Add("style", "width:50px");

      this.InitializeVariables();

      this.PollObjectRow1.Visible = (this.PageContext.IsAdmin || PageContext.BoardSettings.AllowUsersImagedPoll) && this.PageContext.ForumPollAccess;

      if (int.TryParse(this.PollExpire.Text.Trim(), out this._daysPollExpire))
      {
        this._datePollExpire = DateTime.UtcNow.AddDays(this._daysPollExpire);
      }

      if (!this.IsPostBack)
      {
          this.AddPageLinks();

          // Admin can attach an existing group if it's a new poll - this.pollID <= 0
          if (this.PageContext.IsAdmin || this.PageContext.IsForumModerator)
          {
              var pollGroup =
                  DB.PollGroupList(this.PageContext.PageUserID, null, this.PageContext.PageBoardID).Distinct(
                      new AreEqualFunc<TypedPollGroup>((id1, id2) => id1.PollGroupID == id2.PollGroupID)).ToList();

              pollGroup.Insert(0, new TypedPollGroup(String.Empty, -1));

              this.PollGroupListDropDown.Items.AddRange(
                  pollGroup.Select(x => new ListItem(x.Question, x.PollGroupID.ToString())).ToArray());
              
              this.PollGroupListDropDown.DataBind();
              this.PollGroupList.Visible = true;
          }
      }
    }

    /// <summary>
    /// The save poll_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="eventArgs">
    /// The event args.
    /// </param>
    protected void SavePoll_Click(object sender, EventArgs eventArgs)
    {
      if (this.PageContext.ForumPollAccess && this.IsInputVerified())
      {
        if (this.GetPollID() == true)
        {
          this.ReturnToPage();
        }
      }
    }

    /// <summary>
    /// Adds page links to the page
    /// </summary>
    private void AddPageLinks()
    {
      this.PageLinks.AddLink(this.PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));
      if (this._categoryId > 0)
      {
        this.PageLinks.AddLink(
          this.PageContext.PageCategoryName, YafBuildLink.GetLink(ForumPages.forum, "c={0}", this._categoryId));
      }

      if (this._returnForum > 0)
      {
        this.PageLinks.AddLink(
          DB.forum_list(this.PageContext.PageBoardID, this._returnForum).Rows[0]["Name"].ToString(), 
          YafBuildLink.GetLink(ForumPages.topics, "f={0}", this._returnForum));
      }

      if (this._forumId > 0)
      {
        this.PageLinks.AddLink(
          DB.forum_list(this.PageContext.PageBoardID, this._returnForum).Rows[0]["Name"].ToString(), 
          YafBuildLink.GetLink(ForumPages.topics, "f={0}", this._forumId));
      }

      if (this._topicId > 0)
      {
        this.PageLinks.AddLink(
          this._topicInfo["Topic"].ToString(), YafBuildLink.GetLink(ForumPages.posts, "t={0}", this._topicId));
      }

      if (this._editMessageId > 0)
      {
        this.PageLinks.AddLink(
          this._topicInfo["Topic"].ToString(), YafBuildLink.GetLink(ForumPages.postmessage, "m={0}", this._editMessageId));
      }

      this.PageLinks.AddLink(this.GetText("POLLEDIT", "EDITPOLL"), string.Empty);
    }

    /// <summary>
    /// Checks access rights for the page
    /// </summary>
    private void CheckAccess()
    {
      if (this._boardId > 0 || this._categoryId > 0)
      {
        // invalid category
        bool categoryVars = this._categoryId > 0 &&
                            (this._topicId > 0 || this._editTopicId > 0 || this._editMessageId > 0 || this.editForumId > 0 ||
                             this._editBoardId > 0 || this._forumId > 0 || this._boardId > 0);

        // invalid board vars
        bool boardVars = this._boardId > 0 &&
                         (this._topicId > 0 || this._editTopicId > 0 || this._editMessageId > 0 || this.editForumId > 0 ||
                          this._editBoardId > 0 || this._forumId > 0 || this._categoryId > 0);
        if (!categoryVars || (!boardVars))
        {
          YafBuildLink.RedirectInfoPage(InfoMessage.Invalid);
        }
      }
      else if (this._forumId > 0 && (!this.PageContext.ForumPollAccess))
      {
        YafBuildLink.RedirectInfoPage(InfoMessage.AccessDenied);
      }
    }

    /// <summary>
    /// The get poll id.
    /// </summary>
    /// <returns>
    /// The get poll id.
    /// </returns>
    private bool? GetPollID()
    {
      if (int.TryParse(this.PollExpire.Text.Trim(), out this._daysPollExpire))
      {
        this._datePollExpire = DateTime.UtcNow.AddDays(this._daysPollExpire);
      }
      // we are just using existing poll
      if (this.PollId != null)
      {
         string questionPath = this.QuestionObjectPath.Text.Trim();
         string questionMime = string.Empty;

         if (questionPath.IsSet())
         {
             long length = 0;
             questionMime = this.GetImageParameters(new Uri(questionPath), out length);
             if (questionMime.IsNotSet())
             {
                 YafContext.Current.AddLoadMessage(
                     YafContext.Current.Localization.GetTextFormatted("POLLIMAGE_INVALID",
                                                                      questionPath));
                 return false;
             }

             if (length > PageContext.BoardSettings.PollImageMaxFileSize * 1024)
             {
                 YafContext.Current.AddLoadMessage(
                       YafContext.Current.Localization.GetTextFormatted(
                           "POLLIMAGE_TOOBIG", length / 1024, PageContext.BoardSettings.PollImageMaxFileSize, questionPath));
                 return false;
             }

             
         }


          DB.poll_update(
          this.PollId, 
          this.Question.Text, 
          this._datePollExpire, 
          this.IsBoundCheckBox.Checked, 
          this.IsClosedBoundCheckBox.Checked, 
          this.AllowMultipleChoicesCheckBox.Checked,
          questionPath, 
          questionMime);

        foreach (RepeaterItem ri in this.ChoiceRepeater.Items)
        {
          string choice = ((TextBox)ri.FindControl("PollChoice")).Text.Trim();
          string chid = ((HiddenField)ri.FindControl("PollChoiceID")).Value;

          string objectPath = ((TextBox)ri.FindControl("ObjectPath")).Text.Trim();

          string choiceImageMime = string.Empty;
          // update choice
          if (objectPath.IsSet())
          {
              long length = 0;
              choiceImageMime = this.GetImageParameters(new Uri(objectPath), out length);
              if (choiceImageMime.IsNotSet())
              {
                  YafContext.Current.AddLoadMessage(
                      YafContext.Current.Localization.GetTextFormatted("POLLIMAGE_INVALID",
                                                                       objectPath.Trim()));
                  return false;
              }

              if (length > PageContext.BoardSettings.PollImageMaxFileSize * 1024)
              {
                  YafContext.Current.AddLoadMessage(
                      YafContext.Current.Localization.GetTextFormatted(
                          "POLLIMAGE_TOOBIG", length/1024, PageContext.BoardSettings.PollImageMaxFileSize, objectPath));
                  return false;
              }
          }


          if (string.IsNullOrEmpty(chid) && !string.IsNullOrEmpty(choice))
          {
            // add choice
              DB.choice_add(this.PollId, choice, objectPath, choiceImageMime);
          }
          else if (!string.IsNullOrEmpty(chid) && !string.IsNullOrEmpty(choice))
          {
              DB.choice_update(chid, choice, objectPath, choiceImageMime);
          }
          else if (!string.IsNullOrEmpty(chid) && string.IsNullOrEmpty(choice))
          {
            // remove choice
            DB.choice_delete(chid);
          }
        }

        return true;
      }
      else 
      {
        // User wishes to create a poll  
        // The value was selected, we attach an existing poll
        if (Convert.ToInt32(this.PollGroupListDropDown.SelectedIndex) > 0)
        {
            int result = DB.pollgroup_attach(
                Convert.ToInt32(this.PollGroupListDropDown.SelectedValue),
                this._topicId,
                this._forumId,
                this._categoryId,
                this._boardId);
            if (result == 1)
            {
                this.PageContext.LoadMessage.Add(this.GetText("POLLEDIT", "POLLGROUP_ATTACHED"));
            }

            return true;
        }

        string questionPath = this.QuestionObjectPath.Text.Trim();
        string questionMime = string.Empty;

        if (questionPath.IsSet())
        {
            long length = 0;
            questionMime = this.GetImageParameters(new Uri(questionPath), out length);
            if (questionMime.IsNotSet())
            {
                YafContext.Current.AddLoadMessage(
                    YafContext.Current.Localization.GetTextFormatted("POLLIMAGE_INVALID",
                                                                     this.QuestionObjectPath.Text.Trim()));
                return false;
            }

            if (length > PageContext.BoardSettings.PollImageMaxFileSize*1024)
            {
                YafContext.Current.AddLoadMessage(
                    YafContext.Current.Localization.GetTextFormatted(
                "POLLIMAGE_TOOBIG", length/1024, PageContext.BoardSettings.PollImageMaxFileSize, questionPath));
            }
        }

          var pollSaveList = new List<PollSaveList>();

          var rawChoices = new string[3, this.ChoiceRepeater.Items.Count];
          int j = 0;
          foreach (RepeaterItem ri in this.ChoiceRepeater.Items)
          {
            rawChoices[0, j] = ((TextBox)ri.FindControl("PollChoice")).Text.Trim();
            rawChoices[1, j] = questionPath;
            rawChoices[2, j] = questionMime;
            j++;
          }

          int? realTopic = this._topicId;

          if (this._topicId == null)
          {
              realTopic = this._editTopicId;
          }

          if (this._datePollExpire == null && (this.PollExpire.Text.Trim().IsSet()))
          {
            this._datePollExpire = DateTime.UtcNow.AddDays(Convert.ToInt32(this.PollExpire.Text.Trim()));
          }

          pollSaveList.Add(
            new PollSaveList(
              this.Question.Text, 
              rawChoices, 
              this._datePollExpire, 
              this.PageContext.PageUserID, 
              realTopic, 
              this._forumId, 
              this._categoryId, 
              this._boardId, 
              questionPath, 
              questionMime, 
              this.IsBoundCheckBox.Checked, 
              this.IsClosedBoundCheckBox.Checked,
              this.AllowMultipleChoicesCheckBox.Checked));
          DB.poll_save(pollSaveList);
          return true;
        }

      return false; // A poll was not created for this topic.
    }

    /// <summary>
    /// The init poll ui.
    /// </summary>
    /// <param name="pollID">
    /// The poll ID.
    /// </param>
    private void InitPollUI(int? pollID)
    {
      // we should get the schema anyway
      this._choices = DB.poll_stats(pollID);
      this._choices.Columns.Add("ChoiceOrderID", typeof(int));

      // First existing values alway 1!
      int existingRowsCount = 1;
      int allExistingRowsCount = this._choices.Rows.Count;

      // we edit existing poll 
      if (this._choices.Rows.Count > 0)
      {
        if ((Convert.ToInt32(this._choices.Rows[0]["UserID"]) != this.PageContext.PageUserID) &&
            (!this.PageContext.IsAdmin) && (!this.PageContext.IsForumModerator))
        {
          YafBuildLink.RedirectInfoPage(InfoMessage.Invalid);
        }


        this.IsBoundCheckBox.Checked = this._choices.Rows[0]["IsBound"].ToType<bool>();
        this.IsClosedBoundCheckBox.Checked = this._choices.Rows[0]["IsClosedBound"].ToType<bool>();
        this.AllowMultipleChoicesCheckBox.Checked = this._choices.Rows[0]["AllowMultipleChoices"].ToType<bool>();

        this.Question.Text = this._choices.Rows[0]["Question"].ToString();
        this.QuestionObjectPath.Text = this._choices.Rows[0]["QuestionObjectPath"].ToString();

        if (this._choices.Rows[0]["Closes"] != DBNull.Value)
        {
          TimeSpan closing = (DateTime)this._choices.Rows[0]["Closes"] - DateTime.UtcNow;

          this.PollExpire.Text = SqlDataLayerConverter.VerifyInt32(closing.TotalDays + 1).ToString();
        }
        else
        {
          this.PollExpire.Text = null;
        }

        foreach (DataRow choiceRow in this._choices.Rows)
        {
          choiceRow["ChoiceOrderID"] = existingRowsCount;

          existingRowsCount++;
        }
      }
      else
      {
         // A new topic is created
         // below check currently if works for topics only, but will do as some things are not enabled 
         if (!CanCreatePoll())
         {
             YafBuildLink.RedirectInfoPage(InfoMessage.AccessDenied);
         }
        // Get isBound value using page variables. They are initialized here.
        int pgidt = 0;

        // If a topic poll is edited or new topic created
        if (this._topicId > 0 && this._topicInfo != null)
        {
          // topicid should not be null here 
          if (!this._topicInfo["PollID"].IsNullOrEmptyDBField())
          {
            pgidt = (int)this._topicInfo["PollID"];

            DataTable pollGroupData = DB.pollgroup_stats(pgidt);

            this.IsBoundCheckBox.Checked = Convert.ToBoolean(pollGroupData.Rows[0]["IsBound"]);
            // this.IsClosedBoundCheckBox.Checked = Convert.ToBoolean(DB.pollgroup_stats(pgidt).Rows[0]["IsClosedBound"]);
          }
        }
        else if (this._forumId > 0 && (!(this._topicId > 0) || (!(this._editTopicId > 0))))
        {
          // forumid should not be null here
          pgidt = (int)DB.forum_list(this.PageContext.PageBoardID, this._forumId).Rows[0]["PollGroupID"];
        }
        else if (this._categoryId > 0)
        {
          // categoryid should not be null here
          pgidt =
            (int)
            DB.category_listread(this.PageContext.PageBoardID, this.PageContext.PageUserID, this._categoryId).Rows[0][
              "PollGroupID"];
        }

        if (pgidt > 0)
        {
          if (Convert.ToInt32(DB.pollgroup_stats(pgidt).Rows[0]["IsBound"]) == 2)
          {
            this.IsBoundCheckBox.Checked = true;
          }

          if (Convert.ToInt32(DB.pollgroup_stats(pgidt).Rows[0]["IsClosedBound"]) == 4)
          {
            this.IsClosedBoundCheckBox.Checked = true;
          }
        }

        // clear the fields...
        this.PollExpire.Text = string.Empty;
        this.Question.Text = string.Empty;
      }

      // we add dummy rows to data table to fill in repeater empty fields   
      int dummyRowsCount = this.PageContext.BoardSettings.AllowedPollChoiceNumber - allExistingRowsCount - 1;
      for (int i = 0; i <= dummyRowsCount; i++)
      {
        DataRow drow = this._choices.NewRow();
        drow["ChoiceOrderID"] = existingRowsCount + i;
        this._choices.Rows.Add(drow);
      }

      // Bind choices repeater
      this.ChoiceRepeater.DataSource = this._choices;
      this.ChoiceRepeater.DataBind();
      this.ChoiceRepeater.Visible = true;

      // Show controls
      this.SavePoll.Visible = true;
      this.Cancel.Visible = true;
      this.PollRow1.Visible = true;
      this.PollRowExpire.Visible = true;
      this.IsClosedBound.Visible = this.IsBound.Visible = PageContext.BoardSettings.AllowUsersHidePollResults || PageContext.IsAdmin || PageContext.IsForumModerator;
      this.tr_AllowMultipleChoices.Visible = PageContext.BoardSettings.AllowMultipleChoices || PageContext.IsAdmin || PageContext.IsForumModerator;  
    }

    /// <summary>
    /// Initializes page context query variables.
    /// </summary>
    private void InitializeVariables()
    {
      this.PageContext.QueryIDs =
        new QueryStringIDHelper(
          new[] { "p", "ra", "ntp", "t", "e", "em", "m", "f", "ef", "c", "ec", "b", "eb", "rf" });

        // we return to a specific place, general token 
        if (this.PageContext.QueryIDs.ContainsKey("ra"))
        {
          this._topicUnapproved = true;
        }

        // we return to a forum (used when a topic should be approved)
        if (this.PageContext.QueryIDs.ContainsKey("f"))
        {
          this._forumId = this._returnForum = (int)this.PageContext.QueryIDs["f"];
        }

        if (this.PageContext.QueryIDs.ContainsKey("t"))
        {
          this._topicId = (int)this.PageContext.QueryIDs["t"];
          this._topicInfo = DB.topic_info(this._topicId);
        }

        if (this.PageContext.QueryIDs.ContainsKey("m"))
        {
          this._editMessageId = (int)this.PageContext.QueryIDs["m"];
        }

        if (this._editMessageId == null)
        {
          if (this.PageContext.QueryIDs.ContainsKey("ef"))
          {
            this._categoryId = (int)this.PageContext.QueryIDs["ef"];
          }

          if (this.editForumId == null)
          {
            if (this.PageContext.QueryIDs.ContainsKey("c"))
            {
              this._categoryId = (int)this.PageContext.QueryIDs["c"];
            }

            if (this._categoryId == null)
            {
              if (this.PageContext.QueryIDs.ContainsKey("ec"))
              {
                this._editCategoryId = (int)this.PageContext.QueryIDs["ec"];
              }

              if (this._editCategoryId == null)
              {
                if (this.PageContext.QueryIDs.ContainsKey("b"))
                {
                  this._boardId = (int)this.PageContext.QueryIDs["b"];
                }

                if (this._boardId == null)
                {
                  if (this.PageContext.QueryIDs.ContainsKey("eb"))
                  {
                    this._editBoardId = (int)this.PageContext.QueryIDs["eb"];
                  }
                }
              }
            }
          }
        }

        // Check if the user has the page access and variables are correct. 
        this.CheckAccess();

        // handle poll
        if (this.PageContext.QueryIDs.ContainsKey("p"))
        {
          // edit existing poll
          this.PollId = (int)this.PageContext.QueryIDs["p"];
          this.InitPollUI(this.PollId);
        }
        else
        {
          // new poll
          this.PollRow1.Visible = true;
          this.InitPollUI(null);
        }

       // YafBuildLink.RedirectInfoPage(InfoMessage.Invalid);
      
    }

    /// <summary>
    /// The params to send.
    /// </summary>
    /// <param name="retliterals">
    /// The retliterals.
    /// </param>
    /// <param name="retvalue">
    /// The retvalue.
    /// </param>
    private void ParamsToSend(out string retliterals, out int? retvalue)
    {
      if (this._editMessageId > 0)
      {
          retliterals = "em";
          retvalue = this._editMessageId;
      }
      else if (this._topicId > 0)
      {
          retliterals = "t";
          retvalue = this._topicId;
      }
      
      else if (this._forumId > 0)
      {
          retliterals = "f";
          retvalue = this._forumId;
      }
      else if (this.editForumId > 0)
      {
          retliterals = "ef";
          retvalue = this.editForumId;
      }
      else if (this._categoryId > 0)
      {
          retliterals = "c";
          retvalue = this._categoryId;
      }
      else if (this._editCategoryId > 0)
      {
          retliterals = "ec";
          retvalue = this._editCategoryId;
      }
      else if (this._boardId > 0)
      {
          retliterals = "b";
          retvalue = this._boardId;
      }
      else if (this._editBoardId > 0)
      {
          retliterals = "eb";
          retvalue = this._editBoardId;
      }
      else
      {
          retliterals = string.Empty;
          retvalue = 0;
      }

      /* else
             {
                 YafBuildLink.RedirectInfoPage(InfoMessage.AccessDenied);
             } */
    }

    /// <summary>
    /// The return to page.
    /// </summary>
    private void ReturnToPage()
    {
      if (this._topicUnapproved)
      {
        // Tell user that his message will have to be approved by a moderator
        string url = YafBuildLink.GetLink(ForumPages.topics, "f={0}", this._returnForum);

        if (Config.IsRainbow)
        {
          YafBuildLink.Redirect(ForumPages.info, "i=1");
        }
        else
        {
          YafBuildLink.Redirect(ForumPages.info, "i=1&url={0}", this.Server.UrlEncode(url));
        }
      }

      // YafBuildLink.Redirect(ForumPages.posts, "m={0}#{0}", this.Request.QueryString.GetFirstOrDefault("m"));      
      string retliterals = string.Empty;
      int? retvalue;

      this.ParamsToSend(out retliterals, out retvalue);

      switch (retliterals)
      {
        case "t":
          YafBuildLink.Redirect(ForumPages.posts, "t={0}", retvalue);
          break;

        case "em":

          YafBuildLink.Redirect(ForumPages.postmessage, "m={0}", retvalue);
          break;

        case "f":

          YafBuildLink.Redirect(ForumPages.topics, "f={0}", retvalue);
          break;
        case "ef":
          YafBuildLink.Redirect(ForumPages.admin_editforum, "f={0}", retvalue);
          break;
        case "c":
          YafBuildLink.Redirect(ForumPages.forum, "c={0}", retvalue);
          break;
        case "ec":
          YafBuildLink.Redirect(ForumPages.admin_editcategory, "c={0}", retvalue);
          break;
        case "b":
          YafBuildLink.Redirect(ForumPages.forum);
          break;
        case "eb":
          YafBuildLink.Redirect(ForumPages.admin_editboard, "b={0}", retvalue);
          break;
        default:
          YafBuildLink.RedirectInfoPage(InfoMessage.Invalid);
          break;
      }
    }

    /// <summary>
    /// Checks if a user can create poll.
    /// </summary>
    /// <returns>
    /// The can create poll.
    /// </returns>
    private bool CanCreatePoll()
    {
        if (this._topicId > 0)
        {
            // admins can add any number of polls
            if (PageContext.IsAdmin || PageContext.IsForumModerator)
            {
                return true;
            }

            int? pollGroupId = null;
            if (!_topicInfo["PollID"].IsNullOrEmptyDBField())
            {
                pollGroupId = Convert.ToInt32(_topicInfo["PollID"]);
            }

            if (pollGroupId == null && PageContext.BoardSettings.AllowedPollNumber > 0 && PageContext.ForumPollAccess)
            {
                return true;
            }
            else
            {
                // TODO: repeating code
                // Remove repeating PollID values   
                var hTable = new Hashtable();
                var duplicateList = new ArrayList();
                DataTable dtPollGroup = DB.pollgroup_stats(pollGroupId);

                foreach (DataRow drow in dtPollGroup.Rows)
                {
                    if (hTable.Contains(drow["PollID"]))
                    {
                        duplicateList.Add(drow);
                    }
                    else
                    {
                        hTable.Add(drow["PollID"], string.Empty);
                    }
                }

                foreach (DataRow dRow in duplicateList)
                {
                    dtPollGroup.Rows.Remove(dRow);
                }

                dtPollGroup.AcceptChanges();

                // frequently used
                int pollNumber = dtPollGroup.Rows.Count;

                return (pollNumber < this.PageContext.BoardSettings.AllowedPollNumber) &&
                       (this.PageContext.BoardSettings.AllowedPollChoiceNumber > 0);
            }
        }
        return true;
    }

    #endregion
  }
}