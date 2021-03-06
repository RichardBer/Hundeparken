<%@ Control Language="c#" AutoEventWireup="True" Inherits="YAF.Pages.members" Codebehind="members.ascx.cs" %>
<%@ Import Namespace="YAF.Classes.Core"%>
<YAF:PageLinks runat="server" ID="PageLinks" />
<YAF:AlphaSort ID="AlphaSort1" runat="server" />
<YAF:Pager runat="server" ID="Pager" OnPageChange="Pager_PageChange" />
<table class="content" width="100%" cellspacing="1" cellpadding="0">
	<tr>
		<td class="header1" colspan="6">
			<YAF:LocalizedLabel ID="LocalizedLabel1" runat="server" LocalizedTag="title" />
		</td>
	</tr>
	<tr>
        <td class="header2">
            Avatar
        </td>
		<td class="header2">
			<img runat="server" id="SortUserName" alt="Sort User Name" style="vertical-align: middle" />
			<asp:LinkButton runat="server" ID="UserName" OnClick="UserName_Click" /></td>
		<td class="header2">
			<img runat="server" id="SortRank" alt="Sort Rank" style="vertical-align: middle" />
			<asp:LinkButton runat="server" ID="Rank" OnClick="Rank_Click" /></td>
		<td class="header2">
			<img runat="server" id="SortJoined" alt="Sort Joined" style="vertical-align: middle" />
			<asp:LinkButton runat="server" ID="Joined" OnClick="Joined_Click" /></td>
		<td class="header2" align="center">
			<img runat="server" id="SortPosts" alt="Sort Posts" style="vertical-align: middle" />
			<asp:LinkButton runat="server" ID="Posts" OnClick="Posts_Click" /></td>
		<td class="header2">
			<asp:Label runat="server" ID="Location" /></td>
	</tr>
	<asp:Repeater ID="MemberList" runat="server">
		<ItemTemplate>
			<tr>
                <td class="post">
					 <img src="<%# this.GetAvatarUrlFromID(Convert.ToInt32(Eval("UserID")))%>" alt="<%# DataBinder.Eval(Container.DataItem,"Name").ToString() %>" title="<%# DataBinder.Eval(Container.DataItem,"Name").ToString() %>" class="avatarimage" />
				</td>
				<td class="post">
					<YAF:UserLink ID="UserProfileLink" runat="server" UserID='<%# Convert.ToInt32(Eval("UserID")) %>' Style = '<%# Eval("Style") %>' />
				</td>
				<td class="post">
					<%# Eval("RankName") %>
				</td>
				<td class="post">
					<%# this.Get<YafDateTime>().FormatDateLong((System.DateTime)((System.Data.DataRowView)Container.DataItem)["Joined"]) %>
				</td>
				<td class="post" align="center">
					<%# String.Format("{0:N0}",((System.Data.DataRowView)Container.DataItem)["NumPosts"]) %>
				</td>
				<td class="post">
					<%# GetStringSafely(YafUserProfile.GetProfile(DataBinder.Eval(Container.DataItem,"Name").ToString()).Location) %>
				</td>
			</tr>
		</ItemTemplate>
	</asp:Repeater>
</table>
<YAF:Pager runat="server" LinkedPager="Pager" OnPageChange="Pager_PageChange" />
<div id="DivSmartScroller">
	<YAF:SmartScroller ID="SmartScroller1" runat="server" />
</div>
