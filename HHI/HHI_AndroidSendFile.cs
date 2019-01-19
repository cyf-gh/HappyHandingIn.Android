using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Android.Support.V7.App;
using Android.Widget;
using hhi_modules;
using stLib_CS;
using stLib_CS.Net;
using Android.Support.Design.Widget;
using Android.Graphics;

namespace HHI {
    public partial class HHI_Android {
            stLib_CS.Net.Client client = null;
        public static void DeleteFolder( string path ) {
            foreach ( string d in Directory.GetFileSystemEntries( path ) ) {
                if ( File.Exists( d ) ) {
                    FileInfo fi = new FileInfo( d );
                    if ( fi.Attributes.ToString().IndexOf( "ReadOnly" ) != -1 )
                        fi.Attributes = FileAttributes.Normal;
                    File.Delete( d );//直接删除其中的文件  
                } else {
                    DirectoryInfo d1 = new DirectoryInfo( d );
                    if ( d1.GetFiles().Length != 0 ) {
                        DeleteFolder( d1.FullName );////递归删除子文件夹
                    }
                    Directory.Delete( d );
                }
            }
        }
        public void CheckSendingFinished( Android.Content.Context context, CoordinatorLayout cl ) {
            if ( client == null ) {
                HHI_Android.ShowSimpleAlertView( context, "提示", "本次打开软件到现在还未发送过文件。" );
            } else {
                if ( !client.IsServerDisconnected() ) {
                    HHI_Android.ShowSimpleAlertView( context, "提示", "目前还在传输中，请等待。" );
                } else {
                    HHI_Android.ShowSimpleAlertView( context, "提示", "传输已完成！" );
                }
            }
        }
        public async Task<int> Send( Android.Content.Context context, CoordinatorLayout cl, string workName, string serverName, string folderName, List<string>paths ) {
            HHI_HandIn hi = GetCurrentHandIn( workName );
            HHI_ServerInfo serverInfo = GetCurrentServerInfo( serverName );

            if ( client == null ) {
                client = new stLib_CS.Net.Client( serverInfo.IP, serverInfo.Port );
            } else {
                if ( client.Connected() ) {
                    if ( ! client.IsServerDisconnected() ) {
                        HHI_Android.ShowSimpleAlertView( context, "提示", "上一次传输还未完成，请稍后尝试。" );
                        return -1;
                    } 
                }
                client.Disconnect();
                client = new stLib_CS.Net.Client( serverInfo.IP, serverInfo.Port );
            }

            //--- Connect ---
            try {
                if( 1 == await client.Connect() ) {
                    HHI_Android.ShowSimpleAlertView( context, "错误", "IP地址不合法" );
                }
            } catch ( Exception ex ) {
                HHI_Android.ShowSimpleAlertView( context, "错误", "连接错误，错误信息：\n" + ex.Message.ToString() );
                return -1;
            }
            // Update state Connected
            Snackbar.Make(  cl, "连接成功！", Snackbar.LengthShort ).Show();

            // Prepare for zip
            // Create path for tmp
            string targetFolderPath = System.IO.Path.Combine( stLib_CS.Compress.tmppath, folderName );

            if ( Directory.Exists( stLib_CS.Compress.tmppath ) ) {
                DeleteFolder( stLib_CS.Compress.tmppath );
            }
            Directory.CreateDirectory( stLib_CS.Compress.tmppath );
            if ( !Directory.Exists( targetFolderPath ) ) {
                Directory.CreateDirectory( targetFolderPath );
            }

            // Update state creating cache
            try {
                foreach ( var path in paths ) {
                    // TODO compress images
                    // Compress and Save
                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InJustDecodeBounds = false;
                    options.InSampleSize = 4;
                    Bitmap bitmap = BitmapFactory.DecodeFile( path, options );
                    var fileStream = new FileStream( System.IO.Path.Combine( targetFolderPath, System.IO.Path.GetFileName( path ) ), FileMode.Create );
                    bitmap.Compress( Bitmap.CompressFormat.Png, 100, fileStream );
                    fileStream.Close();
                    // File.Copy( path, System.IO.Path.Combine( targetFolderPath, fileName ) );
                }
            } catch ( Exception ex ) {
                client.Disconnect();
                HHI_Android.ShowSimpleAlertView( context, "错误", "临时文件复制错误，错误信息：\n" + ex.Message.ToString() );
                return -1;
            }
            Snackbar.Make(  cl, "临时文件创建成功！", Snackbar.LengthShort ).Show();
            // Compress Zip File
            if ( !Compress.DoZipFile( Compress.tmpname, targetFolderPath ) ) {
                client.Disconnect();
                HHI_Android.ShowSimpleAlertView( context, "错误", "打包临时文件时出现错误" );
                return -1;
            }
            Snackbar.Make(  cl, "缓存压缩成功！", Snackbar.LengthShort ).Show();

            // Check Zip File openable
            FileInfo zip = new FileInfo( Compress.tmpname );
            FileStream zipStream;
            try {
                zipStream = zip.OpenRead();
            } catch ( Exception ex ) {
                client.Disconnect();
                HHI_Android.ShowSimpleAlertView( context, "错误", "压缩包缓存无法打开，错误信息：\n" + ex.Message.ToString() );
                return -1;
            }
            Snackbar.Make(  cl, "压缩缓存校验成功！", Snackbar.LengthShort ).Show();
            // Send File
            NStream stream = client.stream;

            try {
                await stream.WriteString( System.IO.Path.Combine( hi.Path, folderName ) );
                System.Threading.Thread.Sleep( 10 );
                await stream.WriteInt64( zip.Length );
                System.Threading.Thread.Sleep( 10 );
                await stream.WriteBigFrom( zipStream );
                System.Threading.Thread.Sleep( 50 );
            } catch ( Exception ex ) {
                client.Disconnect();
                HHI_Android.ShowSimpleAlertView( context, "错误", "网络流写错误，错误信息：\n" + ex.Message.ToString() );
                return -1;
            }
            Snackbar.Make( cl, "传输完成，等待服务器接受完毕！结果可长按按钮查看！", Snackbar.LengthLong ).Show();
            return 0;
        }
    }
}
