using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using Android.Support.V7.App;
using hhi_modules;

namespace HHI {
    public partial class HHI_Android {
        /// <summary>
        /// Whether is uploading file.
        /// </summary>
        /// <value><c>true</c> if is uploading; otherwise, <c>false</c>.</value>
        public bool IsUploading { get; set; }
        public string CurrentFolderName { get; set; }
        private HHI_DataFeeder mDataFeeder = new HHI_DataFeeder();

        public HHI_AppInfo GetAppInfo() {
            HHI_AppInfo appInfo = new HHI_AppInfo();
            appInfo.CurrentVer = 1001;
            appInfo.NetProtocolVer = 1;
            return appInfo;
        }
        public string CheckVer() {
            HHI_AppInfo curAppInfo = GetAppInfo();
            if ( curAppInfo.CurrentVer >= HHI_Module.AppInfo_Latest.CurrentVer ) {
                return "uptodate";
            } else if ( curAppInfo.CurrentVer < HHI_Module.AppInfo_Latest.CurrentVer ) {
                if ( curAppInfo.CurrentVer < HHI_Module.AppInfo_Min.CurrentVer || curAppInfo.NetProtocolVer != HHI_Module.AppInfo_Latest.NetProtocolVer ) {
                    return "obsoleted";
                } 
            }
            return HHI_Module.AppInfo_Latest.DownloadableSite;
        }

        public HHI_Android() {
            IsUploading = false;
            CurrentFolderName = "";
        }
        /// <summary>
        /// Inits all.
        /// </summary>
        public void InitAll() {

        }

        public void FetchInfos() {
            var xmls = mDataFeeder.GetCompleteXmlFromNet();
            HHI_Module.listHandInData.Clear();
            HHI_Module.listPrefixes.Clear();
            HHI_Module.listServerInfos.Clear();

            HHI_Module.LoadHandIns( xmls[HHI_Module.HHIRootNodeName] );
            HHI_Module.LoadPrefixs( xmls[HHI_Module.PrefixRootNodeName] );
            HHI_Module.LoadConfig( xmls[HHI_Module.ServerInfoRootNodeName] );
            HHI_Module.LoadAppInfo( xmls[HHI_Module.AppInfoNodeName] );
        }
        public HHI_HandIn GetCurrentHandIn( string selectingWorkName ) {
            foreach ( var item in HHI_Module.listHandInData ) {
                if ( item.Name == selectingWorkName ) {
                    return item;
                }
            }
            return null;
        }

        public HHI_Prefix GetCurrentHandInPrefix( string selectingWorkName ) {
            foreach ( var item in HHI_Module.listPrefixes ) {
                if ( item.Name == GetCurrentHandIn( selectingWorkName ).PrefixName ) {
                    return item;
                }
            }
            return null;
        }

        public HHI_ServerInfo GetCurrentServerInfo( string selectingServerName ) {
            foreach ( var item in hhi_modules.HHI_Module.listServerInfos ) {
                if ( item.Name == selectingServerName ) {
                    return item;
                }
            }
            return null;
        }

        public static void ShowSimpleAlertView( Android.Content.Context context, string title, string text ) {
            AlertDialog.Builder builder = new AlertDialog.Builder( context );
            builder.SetTitle( title );
            builder.SetMessage( text );
            builder.SetPositiveButton( Resource.String.str_cn_confrim, ( sender, e ) => { } );
            builder.Create().Show();
        }
        /// <summary>
        /// Shows the confirm alert view.
        /// </summary>
        /// <returns><c>true</c>, if ok is clicked, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        /// <param name="title">Title.</param>
        /// <param name="text">Text.</param>
        public static bool ShowConfirmAlertView( Android.Content.Context context, string title, string text ) {
            bool OK = false;
            AlertDialog.Builder builder = new AlertDialog.Builder( context );
            builder.SetTitle( title );
            builder.SetMessage( text );
            builder.SetPositiveButton( Resource.String.str_cn_confrim,(sender, e) => {
                OK = true;
            } );
            builder.SetNegativeButton( Resource.String.str_cn_cancel, ( sender, e ) => {
                OK = false;
            } );    
            builder.SetCancelable( true );
            builder.Create().Show();
            return OK;
        }
    }

    public class HHI_DataFeeder { 
        public const string Src = "https://www.cnblogs.com/PROJECT-IDOLPROGRAM/p/10225371.html";

        public Dictionary<string, string> GetCompleteXmlFromNet() {
            stLib_CS.Net.WebCrawler c = new stLib_CS.Net.WebCrawler();
            string html = c.GetWebRequest( Src );
            Dictionary<string, string> Xmls = new Dictionary<string, string>();

            string[] strs = html.Split( '~' );

            XmlDocument doc = new XmlDocument();
            doc.LoadXml( WebUtility.HtmlDecode( strs[2] ) );
            XmlNode rootNode = doc.SelectSingleNode( "cyfxml" );
            foreach ( XmlNode node in rootNode.ChildNodes ) {
                Xmls.Add( node.Name, node.OuterXml );
            }
            return Xmls;
        }
    }
}
