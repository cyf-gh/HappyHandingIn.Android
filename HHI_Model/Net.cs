using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Net.NetworkInformation;


namespace stLib_CS {
    namespace Net {
        public class FileTrans {
            public FileTrans( ref TcpClient tcpClient, ref NetworkStream networkStream ) {
                m_tClient = tcpClient;
                ns = networkStream;
            }
            private TcpClient m_tClient;
            private NetworkStream ns;

            public async Task<int> DownloadFiles( string ToPath ) {
                System.Threading.Thread.Sleep( 100 );
                Int32 nfileCount;
                {

                    byte[] fileCount = new byte[4]; //int32
                    await ns.ReadAsync( fileCount, 0, 4 ); // Read 1

                    nfileCount = BitConverter.ToInt32( fileCount, 0 );
                }
                for( int i = 0; i < nfileCount; i++ ) {
                    // 获得文件信息
                    long fileLength;
                    string fileName;
                    {
                        byte[] fileNameBytes;
                        byte[] fileNameLengthBytes = new byte[4]; //int32
                        byte[] fileLengthBytes = new byte[8]; //int64

                        await ns.ReadAsync( fileLengthBytes, 0, 8 ); // int64
                        await ns.ReadAsync( fileNameLengthBytes, 0, 4 ); // int32

                        fileNameBytes = new byte[BitConverter.ToInt32( fileNameLengthBytes, 0 )];
                        await ns.ReadAsync( fileNameBytes, 0, fileNameBytes.Length );


                        fileLength = BitConverter.ToInt64( fileLengthBytes, 0 );
                        fileName = Encoding.BigEndianUnicode.GetString( fileNameBytes );
                    }
                    if( !System.IO.Directory.Exists( ToPath ) ) {
                        System.IO.Directory.CreateDirectory( ToPath );
                    }

                    FileStream fileStream = System.IO.File.Open( System.IO.Path.Combine( ToPath, fileName ), FileMode.Create );

                    int read;
                    int totalRead = 0;
                    byte[] buffer = new byte[32 * 1024]; // 32k 的块
                    while( ( read = await ns.ReadAsync( buffer, 0, buffer.Length ) ) > 0 ) {
                        await fileStream.WriteAsync( buffer, 0, read );
                        totalRead += read;

                        if( totalRead >= fileLength ) {
                            break;
                        }
                    }
                    fileStream.Close();
                }
                return 0;
            }
            public async Task<int> SendFiles( string path ) {
                List<FileInfo> files = stLib_CS.File.FileHelper.GetFiles( path );
                {
                    byte[] fileCount = BitConverter.GetBytes( files.Count );
                    await ns.WriteAsync( fileCount, 0, fileCount.Length );
                }

                foreach( var file in files ) {
                    // 发送文件信息
                    // lbMsg.Text = "发送文件信息 ...";
                    System.Threading.Thread.Sleep( (int)100 );

                    FileStream fileStream;
                    try {
                        fileStream = file.OpenRead();
                    } catch( Exception e ) {
                        return 0;
                    }
                    {
                        byte[] fileName = Encoding.BigEndianUnicode.GetBytes( file.Name );
                        byte[] fileNameLength = BitConverter.GetBytes( fileName.Length );
                        byte[] fileLength = BitConverter.GetBytes( file.Length );
                        await ns.WriteAsync( fileLength, 0, fileLength.Length );
                        await ns.WriteAsync( fileNameLength, 0, fileNameLength.Length );
                        await ns.WriteAsync( fileName, 0, fileName.Length );
                    }

                    // 发送
                    // lbMsg.Text = "发送中 ...";
                    int read;
                    int totalWritten = 0;
                    byte[] buffer = new byte[32 * 1024]; // 32k chunks
                    while( ( read = await fileStream.ReadAsync( buffer, 0, buffer.Length ) ) > 0 ) {
                        await ns.WriteAsync( buffer, 0, read );
                        totalWritten += read;
                    }
                    fileStream.Close(); // .Dispose();
                }
                return 0;
            }
        }
        public class NStream {
            public NStream( ref NetworkStream ns ) {
                m_tNetworkStream = ns;
                ns.ReadTimeout = 10000;
                ns.WriteTimeout = 10000;
                WriteCount = 0;
                ReadCount = 0;
            }
            public Int64 WriteCount { get; set; }
            public Int64 ReadCount { get; set; }
            public NetworkStream m_tNetworkStream;
            private void AddWRef() { ++WriteCount; }
            private void AddRRef() { ++ReadCount; }

            public async Task<int> WriteBigFrom( Stream stream ) {
                byte[] buffer = new byte[32 * 1024]; // 32k chunks

                int numBytesToRead = (int)stream.Length;
                int numBytesRead = 0;
                int n;
                while( numBytesToRead > 0 ) {
                    // int toRead = Math.Min( buffer.Length, numBytesToRead );
                    n = await stream.ReadAsync( buffer, 0, buffer.Length );
                    numBytesRead += n;
                    numBytesToRead -= n;
                    if( n == 0 || numBytesRead > (int)stream.Length )
                        break;

                    await m_tNetworkStream.WriteAsync( buffer, 0, n );
                    AddWRef();
                }
                stream.Flush();
                stream.Close();
                return 0;
            }

            public async Task<int> ReadBigTo( Stream stream, long length ) {
                byte[] buffer = new byte[32 * 1024]; // 32k chunks

                int numBytesToRead = (int)length;
                int numBytesRead = 0;
                int n;
                while( numBytesToRead > 0 ) {
                    n = await m_tNetworkStream.ReadAsync( buffer, 0, buffer.Length );
                    numBytesRead += n;
                    numBytesToRead -= n;
                    if( n == 0 || numBytesRead > length )
                        break;

                    await stream.WriteAsync( buffer, 0, n );
                    AddRRef();
                }

                stream.Flush();
                stream.Close();
                return 0;
            }

            public async Task<Int32> WriteInt64( long n ) {
                byte[] tBytes = BitConverter.GetBytes( n );
                await m_tNetworkStream.WriteAsync( tBytes, 0, tBytes.Length );
                AddWRef();
                return tBytes.Length;
            }
            public async Task<Int64> ReadInt64() {
                byte[] tBytes = new byte[sizeof( Int64 )];
                await m_tNetworkStream.ReadAsync( tBytes, 0, sizeof( Int64 ) );
                AddRRef();
                return BitConverter.ToInt64( tBytes, 0 );
            }
            public async Task<Int32> WriteInt32( int n ) {
                byte[] tBytes = BitConverter.GetBytes( n );
                await m_tNetworkStream.WriteAsync( tBytes, 0, tBytes.Length );
                AddWRef();
                return tBytes.Length;
            }
            public async Task<Int32> ReadInt32() {
                byte[] tBytes = new byte[sizeof( Int32 )];
                await m_tNetworkStream.ReadAsync( tBytes, 0, sizeof( Int32 ) );
                AddRRef();
                return BitConverter.ToInt32( tBytes, 0 );
            }

            public async Task<Int32> WriteString( string str ) {
                byte[] messageBytes = Encoding.Unicode.GetBytes( str );
                await this.WriteInt32( messageBytes.Length );
                await m_tNetworkStream.WriteAsync( messageBytes, 0, messageBytes.Length );
                AddWRef();
                return messageBytes.Length;
            }
            public async Task<string> ReadString() {
                int length = await this.ReadInt32();
                byte[] tBytes = new byte[length];
                await m_tNetworkStream.ReadAsync( tBytes, 0, (int)length );
                AddRRef();
                return Encoding.Unicode.GetString( tBytes );
            }
        }
        public class Ping {
            private PingReply pr;
            public Ping( string host ) {
                System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                pr = ping.Send( host );
            }
            public bool IsSuccess() {
                if( pr.Status == IPStatus.Success ) {
                    return true;
                }
                return false;
            }
            public long GetRoundTime() {
                return pr.RoundtripTime;
            }
        }
        public class Server {
            public NStream stream;
            private TcpListener m_tListener;
            private TcpClient m_tClient;
            public FileTrans fileTrans;
            public Server( string ip = "127.0.0.1", Int32 port = 233 ) {
                m_tListener = new TcpListener( IPAddress.Parse( ip ), port );
            }
            public void SetTimeout( int timeout = 1000 ) {
                stream.m_tNetworkStream.WriteTimeout = timeout;
            }
            public async Task<int> WaitForConnect() {
                m_tListener.Start();

                Console.WriteLine( "Waiting For Connect." );
                m_tClient = await m_tListener.AcceptTcpClientAsync();
                Console.WriteLine( "Connected." );
                NetworkStream ns = m_tClient.GetStream();
                stream = new NStream( ref ns );
                fileTrans = new FileTrans( ref m_tClient, ref ns );
                return 0;
            }
        }
        public class Client {
            public NStream stream;
            public string IPAddress { get; set; }
            public string Port { get; set; }
            private TcpClient m_tClient;
            public FileTrans fileTrans;
            public bool IsServerDisconnected() {
                if ( m_tClient.Client.Poll( -1, SelectMode.SelectRead ) ) {
                    byte[] b = new byte[1];
                    int nRead = m_tClient.Client.Receive( b );
                    if ( nRead == 0 ) {
                        //socket连接已断开
                        return true;
                    }
                }
                return false;
            }
            public Client( string ipa, string port ) {
                IPAddress = ipa;
                Port = port;
            }
            public bool Connected() {
                return m_tClient.Connected;
            }
            public async Task<int> Connect() {
                IPAddress ipAddress;

                if( !System.Net.IPAddress.TryParse( IPAddress, out ipAddress ) ) {
                    return 1;
                }

                m_tClient = new TcpClient();
                try {
                    await m_tClient.ConnectAsync( ipAddress, Convert.ToInt32( Port ) );
                } catch( Exception e ) {
                    throw e;
                }
                NetworkStream ns = m_tClient.GetStream();
                stream = new NStream( ref ns );
                fileTrans = new FileTrans( ref m_tClient, ref ns );
                return 0;
            }
            public void Disconnect() {
                m_tClient.Close();
            }
        }
        public class WebCrawler {
            public string GetWebClient( string url ) {
                string strHTML = "";
                WebClient myWebClient = new WebClient();
                Stream myStream = myWebClient.OpenRead( url );
                StreamReader sr = new StreamReader( myStream, System.Text.Encoding.GetEncoding( "utf-8" ) );
                strHTML = sr.ReadToEnd();
                myStream.Close();
                return strHTML;
            }
            public string GetWebRequest( string url ) {
                Uri uri = new Uri( url );
                WebRequest myReq = WebRequest.Create( uri );
                WebResponse result = myReq.GetResponse();
                Stream receviceStream = result.GetResponseStream();
                StreamReader readerOfStream = new StreamReader( receviceStream, System.Text.Encoding.GetEncoding( "utf-8" ) );
                string strHTML = readerOfStream.ReadToEnd();
                readerOfStream.Close();
                receviceStream.Close();
                result.Close();
                return strHTML;
            }
            public string GetHttpWebRequest( string url ) {
                HttpWebResponse result;
                string strHTML = string.Empty;
                try {
                    Uri uri = new Uri( url );
                    WebRequest webReq = WebRequest.Create( uri );
                    WebResponse webRes = webReq.GetResponse();

                    HttpWebRequest myReq = (HttpWebRequest)webReq;
                    myReq.UserAgent = "User-Agent:Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705";
                    myReq.Accept = "*/*";
                    myReq.KeepAlive = true;
                    myReq.Headers.Add( "Accept-Language", "zh-cn,en-us;q=0.5" );
                    result = (HttpWebResponse)myReq.GetResponse();
                    Stream receviceStream = result.GetResponseStream();
                    StreamReader readerOfStream = new StreamReader( receviceStream, System.Text.Encoding.GetEncoding( "utf-8" ) );
                    strHTML = readerOfStream.ReadToEnd();
                    readerOfStream.Close();
                    receviceStream.Close();
                    result.Close();
                } catch {
                    Uri uri = new Uri( url );
                    WebRequest webReq = WebRequest.Create( uri );
                    HttpWebRequest myReq = (HttpWebRequest)webReq;
                    myReq.UserAgent = "User-Agent:Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705";
                    myReq.Accept = "*/*";
                    myReq.KeepAlive = true;
                    myReq.Headers.Add( "Accept-Language", "zh-cn,en-us;q=0.5" );
                    //result = (HttpWebResponse)myReq.GetResponse();  
                    try {
                        result = (HttpWebResponse)myReq.GetResponse();
                    } catch( WebException ex ) {
                        result = (HttpWebResponse)ex.Response;
                    }
                    Stream receviceStream = result.GetResponseStream();
                    StreamReader readerOfStream = new StreamReader( receviceStream, System.Text.Encoding.GetEncoding( "gb2312" ) );
                    strHTML = readerOfStream.ReadToEnd();
                    readerOfStream.Close();
                    receviceStream.Close();
                    result.Close();
                }
                return strHTML;
            }
        }
    }
}