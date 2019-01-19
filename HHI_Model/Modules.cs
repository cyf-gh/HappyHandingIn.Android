﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace hhi_modules {
    public class HHI_HandIn {
        public bool Closed { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsSubItemFolder { get; set; }
        public string Path { get; set; }
        public string Regex { get; set; }
        public string PrefixName { get; set; }
        public string Desc { get; set; 
        }
    }
    public class HHI_Prefix {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public List<int> Numbers { get; set; }
        public string Include { get; set; }
        public string Exclude { get; set; }
        public List<string> AllMemberName { get; set; }
    }
    public class HHI_ServerInfo {
        public int ID { get; set; }
        public string Name { get; set; }
        public string IP{ get; set; }
        public string Port{ get; set; }
    }
    public class HHI_AppInfo { 
        public int CurrentVer { get; set; }
        public int NetProtocolVer { get; set; }
        public string DownloadableSite { get; set; }
    }
    public static class HHI_Module {
        public static List<HHI_HandIn> listHandInData = new List<HHI_HandIn>();
        public static List<HHI_Prefix> listPrefixes = new List<HHI_Prefix>();
        public static HHI_AppInfo AppInfo_Latest { get; set; } = new HHI_AppInfo();
        public static HHI_AppInfo AppInfo_Min { get; set; } = new HHI_AppInfo();

        public static string m_currentPath = Environment.CurrentDirectory;
        private static string m_logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "log");
        public static string m_dataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "data");
        public static string m_prefixPath = System.IO.Path.Combine(m_dataPath, "prefixs.xml");
        public const string PrefixRootNodeName = "prefixs";
        public const string HHIRootNodeName = "hhis";
        public const string AppInfoNodeName = "appinfos";

        public static string m_handInPath = System.IO.Path.Combine(m_dataPath, "hhi.xml");
        private static XmlDocument m_prefixxml = new XmlDocument();
        private static  XmlDocument m_handInxml = new XmlDocument();
        public static HHI_ServerInfo Current = null;
        public static List<HHI_ServerInfo> listServerInfos = new List<HHI_ServerInfo>();
        public const string ServerInfoRootNodeName = "configs";

        public static void LoadAppInfo( string xml ) {
            XmlDocument XMLConfig = new XmlDocument();
            XMLConfig.LoadXml( xml );
            XmlNode rootNode = XMLConfig.SelectSingleNode( AppInfoNodeName );
            XmlNodeList firstLevelNodeList = rootNode.ChildNodes;
            //</configs>
            foreach ( XmlNode node in firstLevelNodeList ) {
                if ( node.Name == "hhi_android_max" ) {
                    AppInfo_Latest = new HHI_AppInfo();
                    AppInfo_Latest.CurrentVer = Convert.ToInt32( node.Attributes["ver"].Value );
                    AppInfo_Latest.NetProtocolVer = Convert.ToInt32( node.Attributes["net_prot_ver"].Value );
                    AppInfo_Latest.DownloadableSite = node.Attributes["ds"].Value;
                }
                if ( node.Name == "hhi_android_min" ) {
                    AppInfo_Min = new HHI_AppInfo();
                    AppInfo_Min.CurrentVer = Convert.ToInt32( node.Attributes["ver"].Value );
                    AppInfo_Min.NetProtocolVer = Convert.ToInt32( node.Attributes["net_prot_ver"].Value );
                    AppInfo_Min.DownloadableSite = node.Attributes["ds"].Value;
                }
            }
        }

        public static void LoadConfig( string xml ) {
            XmlDocument XMLConfig = new XmlDocument();
            XMLConfig.LoadXml( xml );
            //<configs>
            XmlNode rootNode = XMLConfig.SelectSingleNode( ServerInfoRootNodeName );
            XmlNodeList firstLevelNodeList = rootNode.ChildNodes;
            //</configs>
            foreach ( XmlNode node in firstLevelNodeList ) {
                HHI_ServerInfo tmp = new HHI_ServerInfo();
                tmp.Name = node.Attributes["name"].Value;
                tmp.Port = node.Attributes["port"].Value;
                tmp.IP = node.Attributes["ip"].Value;
                listServerInfos.Add( tmp );
            }
        }
        public static bool IndexExist( string name, HHI_Prefix prefix, out string index ) {
            string studentIndex = "";
            try {
                studentIndex = HHI_Module.FileNameToIndex( name );

            } catch( Exception e ) {
                // MessageBox.Show( "路径："+studentIndex+ "未知错误\n\n" + e.ToString() );
            }
            if( studentIndex == "" ) {
                // MessageBox.Show( "未检测到文件夹名种有学号 \n 学号有误，请重新检查", "错误" );
                index = studentIndex;
                return false;
            }
            int nStudentIndex = Convert.ToInt32( studentIndex );
            index = studentIndex;
            if( !stLib_CS.Generic.ListHelper.IsIn( HHI_Module.GetPrefixIndexList( prefix ), nStudentIndex ) ) {
                // MessageBox.Show( "目前的学号为：" + studentIndex + " 学号有误，请重新检查", "错误" );
                return false;
            }
            return true;
        }
        private static void loadPrefix() {
            XmlNode rootNode = m_prefixxml.SelectSingleNode( PrefixRootNodeName );
            XmlNodeList firstLevelNodeList = rootNode.ChildNodes;
            //<prefix>
            int i = 0;
            foreach ( XmlNode node in firstLevelNodeList ) {
                HHI_Prefix tmp = new HHI_Prefix();
                tmp.Name = node.Attributes["name"].Value;
                string[] names = node.Attributes["namelist"].Value.Split( ';' );
                tmp.AllMemberName =  new List<string>( names );
                // get infos
                XmlNodeList prefixInfos = node.ChildNodes;
                tmp.Start = Convert.ToInt32( prefixInfos.Item( 0 ).InnerText );
                tmp.End = Convert.ToInt32( prefixInfos.Item( 1 ).InnerText );
                tmp.Include = prefixInfos.Item( 2 ).InnerText;
                tmp.Exclude = prefixInfos.Item( 3 ).InnerText;
                tmp.ID = i;
                listPrefixes.Add( tmp );
                ++i;
            }
        }
        public static void LoadPrefixs( string prefixXml ) {
            m_prefixxml.LoadXml( prefixXml );
            loadPrefix();
        }

        public static void LoadPrefixs() {
            m_prefixxml.Load( m_prefixPath );
            loadPrefix();
        }

        private static void loadHandIn() {
            //<prefixs>
            XmlNode rootNode = m_handInxml.SelectSingleNode( "hhis" );
            XmlNodeList firstLevelNodeList = rootNode.ChildNodes;
            //<prefix>
            int i = 0;
            foreach ( XmlNode node in firstLevelNodeList ) {
                HHI_HandIn tmp = new HHI_HandIn();
                tmp.Name = node.Attributes["name"].Value;
                tmp.IsSubItemFolder = Convert.ToBoolean( node.Attributes["isSubItemFolder"].Value );
                tmp.Desc = node.Attributes["desc"].Value.Replace( "\\n", "\n" );
                // get infos
                XmlNodeList prefixInfos = node.ChildNodes;
                tmp.Path = prefixInfos.Item( 0 ).InnerText;
                tmp.Regex = prefixInfos.Item( 1 ).InnerText;
                tmp.PrefixName = prefixInfos.Item( 2 ).InnerText;
                tmp.ID = i;
                listHandInData.Add( tmp );
                ++i;
            }
        }
        public static void LoadHandIns( string xml ) {
            m_handInxml.LoadXml( xml );
            loadHandIn();
        }

        public static void LoadHandIns() {
            m_handInxml.Load( m_handInPath );
            loadHandIn();
        }

        public static void SaveHandInDatum() {
            XmlDocument handinXML = new XmlDocument();
            XmlElement rootElement = handinXML.CreateElement( HHIRootNodeName );
            handinXML.AppendChild( rootElement );
            foreach( var item in listHandInData ) {
                XmlElement hhi = handinXML.CreateElement( "hhi" );
                hhi.SetAttribute( "name", item.Name );
                hhi.SetAttribute( "isSubItemFolder", item.IsSubItemFolder.ToString() );

                XmlElement path = handinXML.CreateElement( "path" );
                path.InnerText = item.Path;
                XmlElement regex = handinXML.CreateElement( "regex" );
                regex.InnerText = item.Regex;
                XmlElement prefix_name = handinXML.CreateElement( "prefix_name" );
                prefix_name.InnerText = item.PrefixName;

                hhi.AppendChild( path );
                hhi.AppendChild( regex );
                hhi.AppendChild( prefix_name );

                rootElement.AppendChild( hhi );
            }
            handinXML.Save( m_handInPath );
        }

        public static void SavePrefixs() {
            XmlDocument handinXML = new XmlDocument();
            XmlElement rootElement = handinXML.CreateElement( "prefixs" );
            handinXML.AppendChild( rootElement );
            foreach( var item in listPrefixes ) {
                XmlElement hhi = handinXML.CreateElement( "prefix" );
                hhi.SetAttribute( "name", item.Name );

                XmlElement start = handinXML.CreateElement( "start_id" );
                start.InnerText = item.Start.ToString();
                XmlElement end = handinXML.CreateElement( "end_id" );
                end.InnerText = item.End.ToString();
                XmlElement include = handinXML.CreateElement( "include" );
                include.InnerText = item.Include;
                XmlElement exclude = handinXML.CreateElement( "exclude" );
                exclude.InnerText = item.Exclude;

                hhi.AppendChild( start );
                hhi.AppendChild( end );
                hhi.AppendChild( include );
                hhi.AppendChild( exclude );

                rootElement.AppendChild( hhi );
            }
            handinXML.Save( m_prefixPath );
        }

        public static void SaveLog( string log ) {
            XmlDocument logXML = new XmlDocument();
            XmlElement rootElement = logXML.CreateElement( "logs" );
            logXML.AppendChild( rootElement );
            rootElement.InnerText = log;

            logXML.Save( m_logPath + System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + GetRandomSeed().ToString() + ".xml" );
        }
        static int GetRandomSeed() {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rng.GetBytes( bytes );
            return BitConverter.ToInt32( bytes, 0 );
        }
        public static List<string> FindFile( string sSourcePath ) {
            List<String> list = new List<string>();
            //遍历文件夹
            DirectoryInfo theFolder = new DirectoryInfo( sSourcePath );
            FileInfo[] thefileInfo = theFolder.GetFiles( "*.*", SearchOption.TopDirectoryOnly );
            foreach( FileInfo NextFile in thefileInfo )  //遍历文件
                list.Add( NextFile.Name );
            //遍历子文件夹
            //DirectoryInfo[] dirInfo = theFolder.GetDirectories();
            //foreach (DirectoryInfo NextFolder in dirInfo) {
            //    //list.Add(NextFolder.ToString());
            //    FileInfo[] fileInfo = NextFolder.GetFiles("*.*", SearchOption.AllDirectories);
            //    foreach (FileInfo NextFile in fileInfo)  //遍历文件
            //        list.Add(NextFile.FullName);
            //}
            return list;
        }

        public static List<string> FindFolders( string sSourcePath ) {
            List<String> list = new List<string>();
            //遍历文件夹
            DirectoryInfo theFolder = new DirectoryInfo( sSourcePath );
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();
            foreach( DirectoryInfo NextFolder in dirInfo ) {
                list.Add( NextFolder.ToString() );
                //FileInfo[] fileInfo = NextFolder.GetFiles("*.*", SearchOption.AllDirectories);
                //foreach (FileInfo NextFile in fileInfo)  //遍历文件
                //list.Add(NextFile.FullName);
            }
            return list;
        }

        public static List<string> GetFileNames( HHI_HandIn hi ) {
            List<string> files;
            // process folder
            if( hi.IsSubItemFolder ) {
                // process file
                files = HHI_Module.FindFolders( hi.Path );
            } else {
                // process file
                files = HHI_Module.FindFile( hi.Path );
            }
            return files;
        }

        public static List<int> GetPrefixIndexList( HHI_Prefix prefix ) {
            List<int> digit = new List<int>();
            for( int i = 0; i < ( prefix.End - prefix.Start + 1 ); i++ ) {
                digit.Add( prefix.Start + i );
            }
            string[] includeIndexs = prefix.Include.Split( ';' );
            string[] excludeIndexs = prefix.Exclude.Split( ';' );

            for( int i = 0; i < digit.Count; i++ ) {
                for( int j = 0; j < excludeIndexs.Count(); j++ ) {
                    if( Convert.ToInt32( excludeIndexs[j] ) == digit[i] ) {
                        digit.RemoveAt( i );
                        break;
                    }
                }
            }

            foreach( var item in includeIndexs ) {
                digit.Add( Convert.ToInt32( item ) );
            }

            return digit;
        }

        public static List<int> GetUnAttachedWorkIndexs( HHI_HandIn hi, HHI_Prefix prefix ) {
            List<int> digit = GetPrefixIndexList( prefix );
            List<string> files = GetFileNames( hi );

            foreach( var file in files ) {
                string strRet = FileNameToIndex( file );
                if( strRet.Equals( "" ) ) {
                    continue;
                }
                for( int j = 0; j < digit.Count; ++j ) {
                    if( digit[j] == Convert.ToInt32( strRet ) ) {
                        digit.Remove( digit[j] );
                        break;
                    }
                }
            }
            return digit;
        }
        public static string FileNameToIndex( string filename ) {
            string pattern = "[0-9]";
            string strRet = "";
            MatchCollection results = Regex.Matches( filename, pattern );
            foreach( var v in results ) {
                strRet += v.ToString();
            }
            return strRet;
        }
    }

}
