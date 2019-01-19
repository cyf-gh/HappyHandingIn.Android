using Android.App;
using Android.OS;
using Android.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Runtime;
using Android.Views;
using Android.Content;
using Android.Support.V4.Widget;
using Android.Support.V4.View;
using Android;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Com.Nguyenhoanglam.Imagepicker.UI.Imagepicker;
using Com.Nguyenhoanglam.Imagepicker.Model;
using Android.Net;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Android.Views.InputMethods;
using System.Threading;
using System;
using Android.Support.Design.Widget;
using Uri = Android.Net.Uri;

namespace HHI {
    [Activity( Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true )]
    public class MainActivity : AppCompatActivity {
        private HHI_Android mHhiAndroid;
        private CoordinatorLayout mCLMain;
        private Button mFABRefresh;
        LinearLayout mLLD;
        private DrawerLayout mDrawerLayout;
        Android.Support.V7.Widget.Toolbar mToolbar;
        ActionBarDrawerToggle mDrawerToggle;

        private Spinner mSpinnerServer;
        private Spinner mSpinnerWork;
        private AutoCompleteTextView mACTVFolderName;
        private TextView mTVMain;
        private Android.Support.Design.Widget.FloatingActionButton mFABSend;
        private ImageAdapter mImageAdapter;
        private GridView mGV;
        private List<Image> mImageList = new List<Image>();
        private bool isFetching = false;
        private ProgressBar mProgressBar;

        protected override void OnCreate( Bundle savedInstanceState ) {
            base.OnCreate( savedInstanceState );
            SetContentView( Resource.Layout.activity_main );

            // Initialize HHI_Android
            mHhiAndroid = new HHI_Android();
            mHhiAndroid.InitAll();


            // Initialize All Controllers 
            InitDrawLayout();
            InitToolbar();
            InitBtnRefresh();
            InitFABSend();
            InitSpinnerServer();
            InitSpinnerWork();
            InitACTVFolderName();
            InitGridView();
            InitPb();
            mTVMain = FindViewById<TextView>( Resource.Id.id_tv_info );
            InitCLMain();
            // 
            LoadInfosFromNet();
        }
        /// <summary>
        /// Inits the toolbar.
        /// </summary>
        protected void InitToolbar() {
            mToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>( Resource.Id.id_toolbar );
            SetSupportActionBar( mToolbar );
            SupportActionBar.SetHomeButtonEnabled( true );
            SupportActionBar.SetDisplayHomeAsUpEnabled( true );
            mDrawerToggle = new ActionBarDrawerToggle( this, mDrawerLayout, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close );
            mDrawerLayout.AddDrawerListener( mDrawerToggle );
            mDrawerToggle.SyncState();
        }

        protected void InitCLMain() {
            mCLMain = FindViewById<CoordinatorLayout>( Resource.Id.id_cl_main );
        }
        protected void LoadInfosFromNet() {
            if ( !isFetching ) {
                isFetching = true;
                try {
                    mHhiAndroid.FetchInfos();
                } catch ( System.Exception ex ) {
                    HHI_Android.ShowSimpleAlertView( this, "错误", "无法获取数据，错误信息\n" + ex.Message.ToString() );
                    isFetching = false;
                    return;
                }
                UpdateSpinnerWork();
                UpdateSpinnerServer();
                UpdateACTVFolderNameAdapater();
                string workname = mSpinnerWork.SelectedItem?.ToString();
                string servername = mSpinnerServer.SelectedItem?.ToString();
                mTVMain.Text = mHhiAndroid.GetCurrentHandIn( workname ).Desc;
                isFetching = false;
                Snackbar.Make( this.Window.DecorView, "服务器信息获取成功！", Snackbar.LengthLong ).Show();
                } else {
                    Snackbar.Make( mCLMain, "正在获取，请稍后", Snackbar.LengthLong ).Show();
                }
        }
        protected void InitPb() {
            mProgressBar = FindViewById<ProgressBar>( Resource.Id.id_pb );
            mProgressBar.Visibility = ViewStates.Gone;
        }

        /// <summary>
        /// ////////////////////////////////////////// GridView //////////////////////////////////////////
        /// </summary>
        protected void InitGridView() {
            mGV = FindViewById<GridView>( Resource.Id.id_rl_main );
            mGV.ItemClick += MGV_ItemClick;
            // toggle with drawlayout ! after init drawlayout
            mImageAdapter = new ImageAdapter( this );
            mGV.Adapter = ( mImageAdapter );
        }

        void MGV_ItemClick( object sender, AdapterView.ItemClickEventArgs e ) {
            AlertDialog.Builder builder = new AlertDialog.Builder( this );
            builder.SetTitle( "提示" );
            builder.SetMessage( "是否要删除这张照片" );
            builder.SetPositiveButton( Resource.String.str_cn_confrim, ( ss, ee ) => {
                mImageAdapter.mImageViewer.PathList.RemoveAt( e.Position );
                mImageAdapter.NotifyDataSetChanged();
            } );
            builder.SetNegativeButton( Resource.String.str_cn_cancel, ( ss, ee ) => { } );
            builder.Create().Show();
        }



        /// <summary>
        /// ////////////////////////////////////////// Refresh button //////////////////////////////////////////
        /// </summary>
        private void InitBtnRefresh() {
            mFABRefresh = FindViewById<Button>( Resource.Id.id_btn_refresh );
            mFABRefresh.Click+=MBtnRefresh_Click;
            mFABRefresh.LongClick += MFABRefresh_LongClick;
        }

        void MFABRefresh_LongClick( object sender, View.LongClickEventArgs e ) {
            string servername = mSpinnerServer.SelectedItem?.ToString();
            if ( servername == "" ) {
                return;
            }
            hhi_modules.HHI_ServerInfo serverInfo = mHhiAndroid.GetCurrentServerInfo( servername );

            HHI_Android.ShowSimpleAlertView( this, "服务器信息", 
            "服务器名：" + servername + "\nIP地址：" + serverInfo.IP + "\n端口号" + serverInfo.Port );

        }


        void MBtnRefresh_Click( object sender, System.EventArgs e ) {
            LoadInfosFromNet();
        }

        /// <summary>
        /// ////////////////////////////////////////// Send button //////////////////////////////////////////
        /// </summary>
        private void InitFABSend() {
            mFABSend = FindViewById<Android.Support.Design.Widget.FloatingActionButton>( Resource.Id.id_fab_send );
            mFABSend.Click+=MFABSend_Click;
            mFABSend.LongClick+=MFABSend_LongClick;
        }

        void MFABSend_LongClick( object sender, View.LongClickEventArgs e ) {
            mHhiAndroid.CheckSendingFinished( this, mCLMain );
        }


        void MFABSend_Click( object sender, System.EventArgs e ) {
            // TODO Popup Window Progress
            string workname = mSpinnerWork.SelectedItem?.ToString();
            string servername = mSpinnerServer.SelectedItem?.ToString();
            string foldername = mACTVFolderName?.Text;
            int fileCount = mImageAdapter.mImageViewer.PathList.Count;

            string note = "";
            if ( workname == "" ) {
                note += "作业名为空\n";
            } 
            if ( servername == "" ) {
                note += "没有选择服务器\n";
            }
            if( foldername == "" ) {
                note += "没有填写姓名+学号\n";
            }
            if ( fileCount == 0 ) {
                note += "没有添加照片\n";
            }

            if ( note != "" ) {
                HHI_Android.ShowSimpleAlertView( this, "提示", note );
                return;
            }
            AlertDialog.Builder builder = new AlertDialog.Builder( this );
            builder.SetTitle( "提示" );
            builder.SetMessage( "即将开始上传，请确认信息：\n"
            + "服务器：" + servername
            + "\n作业：" + workname
            + "\n文件夹名：" + mACTVFolderName.Text
            + "\n照片数量" + fileCount.ToString() + "\n\n确认上传？" );

            builder.SetPositiveButton( Resource.String.str_cn_confrim, async( ss, ee ) => {
                try {
                    mProgressBar.Visibility = ViewStates.Visible;
                    if ( await mHhiAndroid.Send( this, mCLMain, workname, servername, foldername, mImageAdapter.mImageViewer.PathList ) == 0 ) {
                        mProgressBar.Visibility = ViewStates.Gone;
                    }
                } catch( Exception ex ) {
                    HHI_Android.ShowSimpleAlertView( this, "提示", 
                    "停止传送，错误信息："+
                        ex.Message.ToString()+ 
                    "\n" +
                        ex.Source.ToString()+
                    "\n" + ex.TargetSite );
                    mProgressBar.Visibility = ViewStates.Gone;
                }
                mProgressBar.Visibility = ViewStates.Gone;
            } );
            builder.SetNegativeButton( Resource.String.str_cn_cancel, ( ss, ee ) => { } );
            builder.Create().Show();
        }
        /// <summary>
        /// ////////////////////////////////////////// Menu buttons //////////////////////////////////////////
        /// </summary>
        public bool OnMenuItemClick( IMenuItem item ) {
            return true;
        }

        public override System.Boolean OnCreateOptionsMenu( IMenu menu ) {
            this.MenuInflater.Inflate( Resource.Layout.layout_main_actionbar, menu );
            return base.OnCreateOptionsMenu( menu );
        }
        /// <summary>
        /// ////////////////////////////////////////// Auto Complete EditText //////////////////////////////////////////
        /// </summary>
        protected void InitACTVFolderName() {
            mACTVFolderName = FindViewById<AutoCompleteTextView>( Resource.Id.id_actv_foldername );
            string workname = mSpinnerWork.SelectedItem?.ToString();

            var adapter = new ArrayAdapter<string>( this, Android.Resource.Layout.SimpleListItem1, new List<string>() );
            mACTVFolderName.Adapter = adapter;
            mACTVFolderName.EditorAction += ( sender, args ) => {
                if ( args.ActionId == ImeAction.Done ) {
                    HideInput();
                    args.Handled = true;
                }
            };
            mACTVFolderName.AfterTextChanged+=MACTVFolderName_AfterTextChanged;
            mACTVFolderName.EditorAction += MACTVFolderName_EditorAction;
        }
        void UpdateACTVFolderNameAdapater() {
            string workname = mSpinnerWork.SelectedItem?.ToString();
            ArrayAdapter<string> a = (ArrayAdapter<string>)mACTVFolderName.Adapter;
            a.Clear();
            foreach ( var i in mHhiAndroid.GetCurrentHandInPrefix( workname )?.AllMemberName ) {
                a.Add( i );
            }
        }
        void MACTVFolderName_EditorAction( object sender, TextView.EditorActionEventArgs e ) {
        }


        void MACTVFolderName_AfterTextChanged( object sender, Android.Text.AfterTextChangedEventArgs e ) {
            /*
            string number = "";
            if ( hhi_modules.HHI_Module.listPrefixes.Count == 0 ) {
                return;
            }
            if ( hhi_modules.HHI_Module.IndexExist( mACTVFolderName.Text, mHhiAndroid.GetCurrentHandInPrefix(mSpinnerWork.SelectedItem.ToString()), out number ) ) {
                mHhiAndroid.CurrentFolderName = mACTVFolderName.Text;
                string toast = string.Format( "你输入了文件名：{0} 学号为：{1}", mHhiAndroid.CurrentFolderName, number );
                Toast.MakeText( this, toast, ToastLength.Long ).Show();
            } else {
                HHI_Android.ShowSimpleAlertView( this, "提示", "输入内容不合法，请重新输入" );
                mACTVFolderName.Text = "";
                mACTVFolderName.RequestFocus();
            }
            */
        }


        /// <summary>
        /// ////////////////////////////////////////// Spinner Work //////////////////////////////////////////
        /// </summary>
        protected void InitSpinnerWork() {
            mSpinnerWork = FindViewById<Spinner>( Resource.Id.id_spinner_work );
            mSpinnerWork.ItemSelected += MSpinnerWork_ItemSelected;
            var adapter = new ArrayAdapter<string>( this, Android.Resource.Layout.SimpleSpinnerItem, workList );
            mSpinnerWork.Adapter = adapter;
        }

        void MSpinnerWork_ItemSelected( object sender, AdapterView.ItemSelectedEventArgs e ) {
            string toast = string.Format( "你选中了作业：{0}", mSpinnerWork.GetItemAtPosition( e.Position ) );
            Toast.MakeText( this, toast, ToastLength.Long ).Show();
            UpdateACTVFolderNameAdapater();
            mTVMain.Text = mHhiAndroid.GetCurrentHandIn( mSpinnerWork.SelectedItem?.ToString() ).Desc;
        }
        List<string> workList = new List<string>();

        protected void UpdateSpinnerWork() {
            ArrayAdapter<string> a = (ArrayAdapter<string>)mSpinnerWork.Adapter;
            a.Clear();
            foreach ( var i in hhi_modules.HHI_Module.listHandInData ) {
                a.Add( i.Name );
            }
            mSpinnerWork.SetSelection( 0 );
            // ( (BaseAdapter)mSpinnerWork.Adapter ).NotifyDataSetChanged();
        }

        /// <summary>
        /// ////////////////////////////////////////// Spinner Server //////////////////////////////////////////
        /// </summary>
        protected void InitSpinnerServer() {
            mSpinnerServer = FindViewById<Spinner>( Resource.Id.id_spinner_server );

            mSpinnerServer.ItemSelected += MSpinnerServer_ItemSelected;
            var adapter = new ArrayAdapter<string>( this, Android.Resource.Layout.SimpleSpinnerItem, ServerList );
            mSpinnerServer.Adapter = adapter;
        }


        List<string> ServerList= new List<string>();

        protected void UpdateSpinnerServer() {
            ArrayAdapter<string> a = (ArrayAdapter<string>)mSpinnerServer.Adapter;

            a.Clear();
            foreach ( var i in hhi_modules.HHI_Module.listServerInfos ) {
                a.Add( i.Name );
            }
            mSpinnerServer.SetSelection( 0 );
            // ((BaseAdapter)mSpinnerServer.Adapter).NotifyDataSetChanged();
        }

        private void MSpinnerServer_ItemSelected( System.Object sender, AdapterView.ItemSelectedEventArgs e ) {
            string toast = string.Format( "你选中了服务器：{0}", mSpinnerServer.GetItemAtPosition( e.Position ) );
            Toast.MakeText( this, toast, ToastLength.Long ).Show();
        }

        /// <summary>
        /// ////////////////////////////////////////// DrawLayout //////////////////////////////////////////
        /// </summary>
        /// Drawlayout and Actionbar
        protected void InitDrawLayout() {
            mDrawerLayout = FindViewById<DrawerLayout>( Resource.Id.id_drawerlayout );
            mLLD = FindViewById<LinearLayout>( Resource.Id.id_ll_d );
            mLLD.Click += LinearLayout_Click;
            mDrawerLayout.DrawerOpened += MDrawerLayout_DrawerOpened;
            mDrawerLayout.Click += MDrawerLayout_Click; 
            mDrawerLayout.DrawerClosed += MDrawLayout_DrawerClosed;
        }

        void LinearLayout_Click( object sender, EventArgs e ) {
            HideInput();
        }


        void MDrawerLayout_Click( object sender, EventArgs e ) {
        }


        void MDrawerLayout_DrawerOpened(object sender, DrawerLayout.DrawerOpenedEventArgs e) {
            mDrawerLayout.Clickable = true;
        }


        void MDrawLayout_DrawerClosed( object sender, DrawerLayout.DrawerClosedEventArgs e ) {
            HideInput();
        }

        void HideInput() {
            stLibCS.Android.AgileFoo.HideInput( this );
        }

        public override System.Boolean OnOptionsItemSelected( IMenuItem item ) {
            switch( item.ItemId ) {
                case Resource.Id.id_action_btn_Add: {
                        ImagePicker.With( this ).SetFolderMode( true )
                                                .SetCameraOnly( false )
                                                .SetFolderTitle( "Album" )
                                                .SetMultipleMode( true )
                                                .SetSelectedImages( mImageList )
                                                .SetMaxSize( 10 )
                                                .Start();
                        // GetImageStreamAsync();
                        break;
                    }
                case Resource.Id.id_action_btn_remove_all: {
                        AlertDialog.Builder builder = new AlertDialog.Builder( this );
                        builder.SetTitle( "提示" );
                        builder.SetMessage( "是否要删除所有已选照片？" );
                        builder.SetPositiveButton( Resource.String.str_cn_confrim, ( ss, ee ) => {
                            mImageAdapter.mImageViewer.RemoveAll();
                            mImageAdapter.NotifyDataSetChanged();
                        } );
                        builder.SetNegativeButton( Resource.String.str_cn_cancel, ( ss, ee ) => { } );
                        builder.Create().Show();
                        break;
                    }
                case Resource.Id.id_action_btn_appinfo: {
                        string curInfo =  "联系作者：cyf-ms@hotmail.com\n\n"+"软件版本：\n" +
                        "\n\t版本号 " + mHhiAndroid.GetAppInfo().CurrentVer.ToString() +
                        "\n\t传输协议版本 " +mHhiAndroid.GetAppInfo().NetProtocolVer.ToString() +"\n\n";

                        string checkResult = mHhiAndroid.CheckVer();
                        AlertDialog.Builder builder = new AlertDialog.Builder( this );
                        builder.SetTitle( "提示" );
                        if ( checkResult == "uptodate") {
                            builder.SetMessage( curInfo + "已是最新版本" );
                            builder.SetPositiveButton( Resource.String.str_cn_confrim, ( sender, e ) => { } );
                        } else if ( checkResult == "obsoleted" ) {
                            builder.SetMessage( curInfo + "当前版本已经被废弃不可使用\n请更新" );
                            builder.SetPositiveButton( Resource.String.str_cn_confrim, ( ss, ee ) => {
                                Intent intent = new Intent();
                                intent.SetAction( "android.intent.action.VIEW" );
                                Uri content_url = Uri.Parse( checkResult );
                                intent.SetData( content_url );
                                StartActivity( intent );
                            } );
                            builder.SetNegativeButton( Resource.String.str_cn_cancel, ( ss, ee ) => { } );
                        } else {
                            builder.SetMessage( curInfo + "有新版本可以使用\n是否更新？" );
                            builder.SetPositiveButton( Resource.String.str_cn_confrim, ( ss, ee ) => {
                                Intent intent = new Intent();
                                intent.SetAction( "android.intent.action.VIEW" );
                                Uri content_url = Uri.Parse( checkResult );
                                intent.SetData( content_url );
                                StartActivity( intent );
                            } );
                            builder.SetNegativeButton( Resource.String.str_cn_cancel, ( ss, ee ) => { } );
                        }
                        builder.Create().Show();
                        break;
                    }
            }
            return base.OnOptionsItemSelected( item ) || mDrawerToggle.OnOptionsItemSelected( item );
        }
        protected override void OnActivityResult( int requestCode, Result resultCode, Android.Content.Intent data ) {
            if ( requestCode == Config.RcPickImages && (int)resultCode == -1 && data != null ) {
                var list = data.GetParcelableArrayListExtra( Config.ExtraImages );
                if ( list != null ) {
                    foreach ( var item in list ) {
                        var img = (Image)item;
                        if ( ! mImageAdapter.mImageViewer.Existed( img.Path ) ) {
                            mImageAdapter.mImageViewer.AddPath( img.Path );
                        }
                    }
                    mImageAdapter.NotifyDataSetChanged();
                }
                // adapter.setData( images );
            }
            base.OnActivityResult( requestCode, resultCode, data );
        }

        /// Some activities
        protected override void OnResume() {
            base.OnResume();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
        }

        protected override void OnStop() {
            base.OnStop();
        }
    }

}