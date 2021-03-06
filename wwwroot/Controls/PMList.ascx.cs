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

namespace YAF.Controls
{
  #region Using

  using System;
  using System.Collections;
  using System.ComponentModel;
  using System.Data;
  using System.IO;
  using System.Text;
  using System.Web;
  using System.Web.UI.WebControls;
  using System.Xml;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Utils;

  #endregion

  /// <summary>
  /// The pm list.
  /// </summary>
  public partial class PMList : BaseUserControl
  {
    #region Properties

    /// <summary>
    ///   Gets or sets the current view for the user's private messages.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets the current view for the user's private messages.")]
    public PMView View
    {
      get
      {
          if (this.ViewState["View"] != null)
        {
          return (PMView)this.ViewState["View"];
        }
          return PMView.Inbox;
      }

        set
      {
        this.ViewState["View"] = value;
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// The archive all_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ArchiveAll_Click(object source, EventArgs e)
    {
      if (this.View != PMView.Inbox)
      {
        return;
      }

      long archivedCount = 0;
      using (DataView dv = DB.pmessage_list(this.PageContext.PageUserID, null, null).DefaultView)
      {
        dv.RowFilter = "IsDeleted = False AND IsArchived = False";

        foreach (DataRowView item in dv)
        {
          DB.pmessage_archive(item["UserPMessageID"]);
          archivedCount++;
        }
      }

      this.BindData();
      this.PageContext.Cache.Remove(YafCache.GetBoardCacheKey(Constants.Cache.ActiveUserLazyData.FormatWith(PageContext.PageUserID)));
      this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("MSG_ARCHIVED+").FormatWith(archivedCount));
    }

    /// <summary>
    /// The archive all_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ArchiveAll_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}')".FormatWith(this.PageContext.Localization.GetText("CONFIRM_ARCHIVEALL"));
    }

    /// <summary>
    /// The archive selected_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ArchiveSelected_Click(object source, EventArgs e)
    {
      if (this.View != PMView.Inbox)
      {
        return;
      }

      long archivedCount = 0;
      foreach (GridViewRow item in this.MessagesView.Rows)
      {
        if (((CheckBox)item.FindControl("ItemCheck")).Checked)
        {
          DB.pmessage_archive(this.MessagesView.DataKeys[item.RowIndex].Value);
          archivedCount++;
        }
      }

      this.BindData();
      this.PageContext.Cache.Remove(YafCache.GetBoardCacheKey(Constants.Cache.ActiveUserLazyData.FormatWith(PageContext.PageUserID)));
        this.PageContext.AddLoadMessage(archivedCount == 1
                                            ? this.PageContext.Localization.GetText("MSG_ARCHIVED")
                                            : this.PageContext.Localization.GetText("MSG_ARCHIVED+").FormatWith(
                                                archivedCount));
    }

    /// <summary>
    /// The date link_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void DateLink_Click(object sender, EventArgs e)
    {
      this.SetSort("Created", false);
      this.BindData();
    }

    /// <summary>
    /// The delete all_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void DeleteAll_Click(object source, EventArgs e)
    {
      long nItemCount = 0;

      object toUserID = null;
      object fromUserID = null;
      bool isoutbox = false;

      if (this.View == PMView.Outbox)
      {
        fromUserID = this.PageContext.PageUserID;
        isoutbox = true;
      }
      else
      {
        toUserID = this.PageContext.PageUserID;
      }

      using (DataView dv = DB.pmessage_list(toUserID, fromUserID, null).DefaultView)
      {
        if (this.View == PMView.Inbox)
        {
          dv.RowFilter = "IsDeleted = False AND IsArchived = False";
        }
        else if (this.View == PMView.Outbox)
        {
          dv.RowFilter = "IsInOutbox = True";
        }
        else if (this.View == PMView.Archive)
        {
          dv.RowFilter = "IsArchived = True";
        }

        foreach (DataRowView item in dv)
        {
          if (isoutbox)
          {
            DB.pmessage_delete(item["UserPMessageID"], true);
          }
          else
          {
            DB.pmessage_delete(item["UserPMessageID"]);
          }

          nItemCount++;
        }
      }

      this.BindData();
      this.PageContext.AddLoadMessage(this.PageContext.Localization.GetTextFormatted("msgdeleted2", nItemCount));
      this.PageContext.Cache.Remove(YafCache.GetBoardCacheKey(Constants.Cache.ActiveUserLazyData.FormatWith(PageContext.PageUserID)));
    }

    /// <summary>
    /// The delete all_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void DeleteAll_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}')".FormatWith(this.PageContext.Localization.GetText("CONFIRM_DELETEALL"));
    }

    /// <summary>
    /// The delete selected_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void DeleteSelected_Click(object source, EventArgs e)
    {
      long nItemCount = 0;

      foreach (GridViewRow item in this.MessagesView.Rows)
      {
        if (((CheckBox)item.FindControl("ItemCheck")).Checked)
        {
          if (this.View == PMView.Outbox)
          {
            DB.pmessage_delete(this.MessagesView.DataKeys[item.RowIndex].Value, true);
          }
          else
          {
            DB.pmessage_delete(this.MessagesView.DataKeys[item.RowIndex].Value);
          }

          nItemCount++;
        }
      }

      this.BindData();

        this.PageContext.AddLoadMessage(nItemCount == 1
                                            ? this.PageContext.Localization.GetText("msgdeleted1")
                                            : this.PageContext.Localization.GetTextFormatted("msgdeleted2", nItemCount));
        this.PageContext.Cache.Remove(YafCache.GetBoardCacheKey(Constants.Cache.ActiveUserLazyData.FormatWith(PageContext.PageUserID)));
    }

    /// <summary>
    /// The delete selected_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void DeleteSelected_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}')".FormatWith(this.PageContext.Localization.GetText("CONFIRM_DELETE"));
    }

    /// <summary>
    /// The delete all_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ExportAll_Click(object source, EventArgs e)
    {
      var messageList = (DataView)this.MessagesView.DataSource;

      // Return if No Messages are Available to Export
      if (messageList.Table.Rows.Count.Equals(0))
      {
        this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("NO_MESSAGES"));
        return;
      }

      if (this.ExportType.SelectedItem.Value.Equals("xml"))
      {
        this.ExportXmlFile(messageList);
      }
      else if (this.ExportType.SelectedItem.Value.Equals("csv"))
      {
        this.ExportCsvFile(messageList);
      }
      else if (this.ExportType.SelectedItem.Value.Equals("txt"))
      {
        this.ExportTextFile(messageList);
      }
    }

    /// <summary>
    /// The delete all_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void ExportSelected_Click(object source, EventArgs e)
    {
      var alNotSelMessages = new ArrayList();

      long nItemCount = 0;

      foreach (GridViewRow item in this.MessagesView.Rows)
      {
        if (((CheckBox)item.FindControl("ItemCheck")).Checked)
        {
          nItemCount++;
        }
        else
        {
          alNotSelMessages.Add(item.DataItemIndex);
        }
      }

      // Return if No Message Selected
      if (nItemCount.Equals(0))
      {
        this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("MSG_NOSELECTED"));

        this.BindData();

        return;
      }

      var messageList = (DataView)this.MessagesView.DataSource;

      foreach (int iItemIndex in alNotSelMessages)
      {
        messageList.Table.Rows.RemoveAt(iItemIndex);
      }

      if (this.ExportType.SelectedItem.Value.Equals("xml"))
      {
        this.ExportXmlFile(messageList);
      }
      else if (this.ExportType.SelectedItem.Value.Equals("csv"))
      {
        this.ExportCsvFile(messageList);
      }
      else if (this.ExportType.SelectedItem.Value.Equals("txt"))
      {
        this.ExportTextFile(messageList);
      }
    }

    /// <summary>
    /// The format body.
    /// </summary>
    /// <param name="o">
    /// The o.
    /// </param>
    /// <returns>
    /// The format body.
    /// </returns>
    protected string FormatBody(object o)
    {
      var row = (DataRowView)o;
      return (string)row["Body"];
    }

    /// <summary>
    /// The from link_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void FromLink_Click(object sender, EventArgs e)
    {
        this.SetSort(this.View == PMView.Outbox ? "ToUser" : "FromUser", true);

        this.BindData();
    }

      /// <summary>
    /// The get image.
    /// </summary>
    /// <param name="o">
    /// The o.
    /// </param>
    /// <returns>
    /// The get image.
    /// </returns>
    protected string GetImage(object o)
    {
      return this.PageContext.Theme.GetItem(
        "ICONS", SqlDataLayerConverter.VerifyBool(((DataRowView)o)["IsRead"]) ? "TOPIC" : "TOPIC_NEW");
    }

    /// <summary>
    /// The get localized text.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The get localized text.
    /// </returns>
    protected string GetLocalizedText(string text)
    {
      return this.HtmlEncode(this.PageContext.Localization.GetText(text));
    }

    /// <summary>
    /// The get message link.
    /// </summary>
    /// <param name="messageId">
    /// The message id.
    /// </param>
    /// <returns>
    /// The get message link.
    /// </returns>
    protected string GetMessageLink(object messageId)
    {
      return YafBuildLink.GetLink(
        ForumPages.cp_message, "pm={0}&v={1}", messageId, PMViewConverter.ToQueryStringParam(this.View));
    }

    /// <summary>
    /// The get message user header.
    /// </summary>
    /// <returns>
    /// The get message user header.
    /// </returns>
    protected string GetMessageUserHeader()
    {
      return this.GetLocalizedText(this.View == PMView.Outbox ? "to" : "from");
    }

    /// <summary>
    /// The get p message text.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <param name="_total">
    /// The _total.
    /// </param>
    /// <param name="_inbox">
    /// The _inbox.
    /// </param>
    /// <param name="_outbox">
    /// The _outbox.
    /// </param>
    /// <param name="_archive">
    /// The _archive.
    /// </param>
    /// <param name="_limit">
    /// The _limit.
    /// </param>
    /// <returns>
    /// The get p message text.
    /// </returns>
    protected string GetPMessageText(
      string text, object _total, object _inbox, object _outbox, object _archive, object _limit)
    {
      object _percentage = 0;
      if (Convert.ToInt32(_limit) != 0)
      {
        _percentage = decimal.Round((Convert.ToDecimal(_total) / Convert.ToDecimal(_limit)) * 100, 2);
      }

      if (YafContext.Current.IsAdmin)
      {
        _limit = "\u221E";
        _percentage = 0;
      }

      return
        this.HtmlEncode(
          this.PageContext.Localization.GetTextFormatted(text, _total, _inbox, _outbox, _archive, _limit, _percentage));
    }

    /// <summary>
    /// The get title.
    /// </summary>
    /// <returns>
    /// The get title.
    /// </returns>
    protected string GetTitle()
    {
        switch (this.View)
        {
            case PMView.Outbox:
                return this.GetLocalizedText("SENTITEMS");
            case PMView.Inbox:
                return this.GetLocalizedText("INBOX");
            default:
                return this.GetLocalizedText("ARCHIVE");
        }
    }

      /// <summary>
    /// The mark as read_ click.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void MarkAsRead_Click(object source, EventArgs e)
    {
      if (this.View == PMView.Outbox)
      {
        return;
      }

      using (DataView dv = DB.pmessage_list(this.PageContext.PageUserID, null, null).DefaultView)
      {
        if (this.View == PMView.Inbox)
        {
          dv.RowFilter = "IsRead = False AND IsDeleted = False AND IsArchived = False";
        }
        else if (this.View == PMView.Archive)
        {
          dv.RowFilter = "IsRead = False AND IsArchived = True";
        }

        foreach (DataRowView item in dv)
        {
          DB.pmessage_markread(item["UserPMessageID"]);

          // Clearing cache with old permissions data...
          this.PageContext.Cache.Remove(
            YafCache.GetBoardCacheKey(Constants.Cache.ActiveUserLazyData.FormatWith(this.PageContext.PageUserID)));
        }
      }

      this.BindData();
    }

    /// <summary>
    /// The messages view_ row created.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void MessagesView_RowCreated(object sender, GridViewRowEventArgs e)
    {
      if (e.Row.RowType == DataControlRowType.Header)
      {
        var oGridView = (GridView)sender;
        var oGridViewRow = new GridViewRow(0, 0, DataControlRowType.Header, DataControlRowState.Insert);

        var oTableCell = new TableCell { Text = this.GetTitle(), CssClass = "header1", ColumnSpan = 5 };

        // Add Header to top with column span of 5... no need for two tables.
        oGridViewRow.Cells.Add(oTableCell);
        oGridView.Controls[0].Controls.AddAt(0, oGridViewRow);

        var SortFrom = (Image)e.Row.FindControl("SortFrom");
        var SortSubject = (Image)e.Row.FindControl("SortSubject");
        var SortDate = (Image)e.Row.FindControl("SortDate");

        SortFrom.Visible = (this.View == PMView.Outbox)
                             ? (string)this.ViewState["SortField"] == "ToUser"
                             : (string)this.ViewState["SortField"] == "FromUser";
        SortFrom.ImageUrl = this.PageContext.Theme.GetItem(
          "SORT", (bool)this.ViewState["SortAsc"] ? "ASCENDING" : "DESCENDING");

        SortSubject.Visible = (string)this.ViewState["SortField"] == "Subject";
        SortSubject.ImageUrl = this.PageContext.Theme.GetItem(
          "SORT", (bool)this.ViewState["SortAsc"] ? "ASCENDING" : "DESCENDING");

        SortDate.Visible = (string)this.ViewState["SortField"] == "Created";
        SortDate.ImageUrl = this.PageContext.Theme.GetItem(
          "SORT", (bool)this.ViewState["SortAsc"] ? "ASCENDING" : "DESCENDING");
      }
      else if (e.Row.RowType == DataControlRowType.Footer)
      {
        int rolCount = e.Row.Cells.Count;

        for (int i = rolCount - 1; i >= 1; i--)
        {
          e.Row.Cells.RemoveAt(i);
        }

        e.Row.Cells[0].ColumnSpan = rolCount;
      }
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
      if (this.ViewState["SortField"] == null)
      {
        this.SetSort("Created", false);
      }

      if (!this.IsPostBack)
      {
        // setup pager...
        this.MessagesView.AllowPaging = true;
        this.MessagesView.PagerSettings.Visible = false;
        this.MessagesView.AllowSorting = true;

        this.PagerTop.PageSize = 10;
        this.MessagesView.PageSize = 10;
      }
      else
      {
        // make sure addLoadMessage is empty...
        this.PageContext.LoadMessage.Clear();
      }

      this.lblExportType.Text = this.PageContext.Localization.GetText("EXPORTFORMAT");

      this.BindData();
    }

    /// <summary>
    /// The pager top_ page change.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void PagerTop_PageChange(object sender, EventArgs e)
    {
      // rebind
      this.BindData();
    }

    /// <summary>
    /// The stats_ renew.
    /// </summary>
    protected void Stats_Renew()
    {
      // Renew PM Statistics
      DataTable dt = DB.user_pmcount(this.PageContext.PageUserID);
      if (dt.Rows.Count > 0)
      {
        this.PMInfoLink.Text = this.GetPMessageText(
          "PMLIMIT_ALL", 
          dt.Rows[0]["NumberTotal"], 
          dt.Rows[0]["NumberIn"], 
          dt.Rows[0]["NumberOut"], 
          dt.Rows[0]["NumberArchived"], 
          dt.Rows[0]["NumberAllowed"]);
      }
    }

    /// <summary>
    /// The subject link_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void SubjectLink_Click(object sender, EventArgs e)
    {
      this.SetSort("Subject", true);
      this.BindData();
    }

    /// <summary>
    /// The bind data.
    /// </summary>
    private void BindData()
    {
      object toUserID = null;
      object fromUserID = null;

      if (this.View == PMView.Outbox)
      {
        fromUserID = this.PageContext.PageUserID;
      }
      else
      {
        toUserID = this.PageContext.PageUserID;
      }

      using (DataView dv = DB.pmessage_list(toUserID, fromUserID, null).DefaultView)
      {
        switch (this.View)
        {
            case PMView.Inbox:
                dv.RowFilter = "IsDeleted = False AND IsArchived = False";
                break;
            case PMView.Outbox:
                dv.RowFilter = "IsInOutbox = True";
                break;
            case PMView.Archive:
                dv.RowFilter = "IsArchived = True";
                break;
        }

        dv.Sort = "{0} {1}".FormatWith(this.ViewState["SortField"], (bool)this.ViewState["SortAsc"] ? "asc" : "desc");
        this.PagerTop.Count = dv.Count;

        if (dv.Count > 0)
        {
            lblExportType.Visible = true;
            ExportType.Visible = true;
        }
        else
        {
            lblExportType.Visible = false;
            ExportType.Visible = false;
        }

        this.MessagesView.PageIndex = this.PagerTop.CurrentPageIndex;
        this.MessagesView.DataSource = dv;
        this.MessagesView.DataBind();
      }

      this.Stats_Renew();
    }

    /// <summary>
    /// Export the Private Messages in messageList as CSV File
    /// </summary>
    /// <param name="messageList">
    /// DataView that Contains the Private Messages
    /// </param>
    private void ExportCsvFile(DataView messageList)
    {
      HttpContext.Current.Response.Clear();
      HttpContext.Current.Response.ClearContent();
      HttpContext.Current.Response.ClearHeaders();

      HttpContext.Current.Response.ContentType = "application/vnd.csv";
      this.Response.AppendHeader(
        "content-disposition", 
        "attachment; filename=" +
        HttpUtility.UrlEncode(
          "Privatemessages-{0}-{1}.csv".FormatWith(
            this.PageContext.PageUserName, DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HHmm"))));

      var sw = new StreamWriter(HttpContext.Current.Response.OutputStream);

      int iColCount = messageList.Table.Columns.Count;

      for (int i = 0; i < iColCount; i++)
      {
        sw.Write(messageList.Table.Columns[i]);

        if (i < iColCount - 1)
        {
          sw.Write(",");
        }
      }

      sw.Write(sw.NewLine);

      foreach (DataRow dr in messageList.Table.Rows)
      {
        for (int i = 0; i < iColCount; i++)
        {
          if (!Convert.IsDBNull(dr[i]))
          {
            sw.Write(dr[i].ToString());
          }

          if (i < iColCount - 1)
          {
            sw.Write(",");
          }
        }

        sw.Write(sw.NewLine);
      }

      sw.Close();

      HttpContext.Current.Response.Flush();
      HttpContext.Current.Response.End();
    }

    /// <summary>
    /// Export the Private Messages in messageList as Text File
    /// </summary>
    /// <param name="messageList">
    /// DataView that Contains the Private Messages
    /// </param>
    private void ExportTextFile(DataView messageList)
    {
      HttpContext.Current.Response.Clear();
      HttpContext.Current.Response.ClearContent();
      HttpContext.Current.Response.ClearHeaders();

      HttpContext.Current.Response.ContentType = "application/vnd.text";
      this.Response.AppendHeader(
        "content-disposition", 
        "attachment; filename=" +
        HttpUtility.UrlEncode(
          "Privatemessages-{0}-{1}.txt".FormatWith(
            this.PageContext.PageUserName, DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HHmm"))));

      var sw = new StreamWriter(HttpContext.Current.Response.OutputStream);

      sw.Write("{0};{1}".FormatWith(YafContext.Current.BoardSettings.Name, YafForumInfo.ForumURL));
      sw.Write(sw.NewLine);
      sw.Write("Private Message Dump for User {0}; {1}".FormatWith(this.PageContext.PageUserName, DateTime.Now));
      sw.Write(sw.NewLine);

      for (int i = 0; i <= messageList.Table.DataSet.Tables[0].Rows.Count - 1; i++)
      {
        for (int j = 0; j <= messageList.Table.DataSet.Tables[0].Columns.Count - 1; j++)
        {
          sw.Write(
            "{0}: {1}", messageList.Table.DataSet.Tables[0].Columns[j], messageList.Table.DataSet.Tables[0].Rows[i][j]);
          sw.Write(sw.NewLine);
        }
      }

      sw.Close();

      HttpContext.Current.Response.Flush();
      HttpContext.Current.Response.End();
    }

    /// <summary>
    /// Export the Private Messages in messageList as Xml File
    /// </summary>
    /// <param name="messageList">
    /// DataView that Contains the Private Messages
    /// </param>
    private void ExportXmlFile(DataView messageList)
    {
      HttpContext.Current.Response.Clear();
      HttpContext.Current.Response.ClearContent();
      HttpContext.Current.Response.ClearHeaders();

      HttpContext.Current.Response.ContentType = "text/xml";
      this.Response.AppendHeader(
        "content-disposition", 
        "attachment; filename=" +
        HttpUtility.UrlEncode(
          "Privatemessages-{0}-{1}.xml".FormatWith(
            this.PageContext.PageUserName, DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HHmm"))));

      messageList.Table.TableName = "PrivateMessage";

      var xwSettings = new XmlWriterSettings
        {
           Encoding = Encoding.UTF8, OmitXmlDeclaration = false, Indent = true, NewLineOnAttributes = true 
        };

      XmlWriter xw = XmlWriter.Create(HttpContext.Current.Response.OutputStream, xwSettings);
      xw.WriteStartDocument();

      messageList.Table.DataSet.DataSetName = "PrivateMessages";

      xw.WriteComment(" {0};{1} ".FormatWith(YafContext.Current.BoardSettings.Name, YafForumInfo.ForumURL));
      xw.WriteComment(
        " Private Message Dump for User {0}; {1} ".FormatWith(this.PageContext.PageUserName, DateTime.Now));

      var xd = new XmlDataDocument(messageList.Table.DataSet);

      foreach (XmlNode node in xd.ChildNodes)
      {
        // nItemCount = node.ChildNodes.Count;
        node.WriteTo(xw);
      }

      xw.WriteEndDocument();

      xw.Close();

      HttpContext.Current.Response.Flush();
      HttpContext.Current.Response.End();
    }

    /// <summary>
    /// The set sort.
    /// </summary>
    /// <param name="field">
    /// The field.
    /// </param>
    /// <param name="asc">
    /// The asc.
    /// </param>
    private void SetSort(string field, bool asc)
    {
      if (this.ViewState["SortField"] != null && (string)this.ViewState["SortField"] == field)
      {
        this.ViewState["SortAsc"] = !(bool)this.ViewState["SortAsc"];
      }
      else
      {
        this.ViewState["SortField"] = field;
        this.ViewState["SortAsc"] = asc;
      }
    }

    #endregion
  }

  /// <summary>
  /// Indicates the mode of the PMList.
  /// </summary>
  public enum PMView
  {
    /// <summary>
    ///   The inbox.
    /// </summary>
    Inbox = 0, 

    /// <summary>
    ///   The outbox.
    /// </summary>
    Outbox, 

    /// <summary>
    ///   The archive.
    /// </summary>
    Archive
  }

  /// <summary>
  /// Converts <see cref="PMView"/>s to and from their URL query string representations.
  /// </summary>
  public static class PMViewConverter
  {
    #region Public Methods

    /// <summary>
    /// Returns a <see cref="PMView"/> based on its URL query string value.
    /// </summary>
    /// <param name="param">
    /// </param>
    /// <returns>
    /// </returns>
    public static PMView FromQueryString(string param)
    {
      if (param.IsNotSet())
      {
        return PMView.Inbox;
      }

      switch (param.ToLower())
      {
        case "out":
          return PMView.Outbox;
        case "in":
          return PMView.Inbox;
        case "arch":
          return PMView.Archive;
        default: // Inbox by default
          return PMView.Inbox;
      }
    }

    /// <summary>
    /// Converts a <see cref="PMView"/> to a string representation appropriate for inclusion in a URL query string.
    /// </summary>
    /// <param name="view">
    /// </param>
    /// <returns>
    /// The to query string param.
    /// </returns>
    public static string ToQueryStringParam(PMView view)
    {
      switch (view)
      {
        case PMView.Outbox:
          return "out";
        case PMView.Inbox:
          return "in";
        case PMView.Archive:
          return "arch";
        default:
          return null;
      }
    }

    #endregion
  }
}