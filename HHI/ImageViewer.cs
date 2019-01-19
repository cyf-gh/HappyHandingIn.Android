using System;
using System.Collections.Generic;
using Android.Widget;
using Android.Graphics;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Java.Lang;
using static Android.Support.V7.Widget.RecyclerView;

namespace HHI {
    public class ImageViewer {
        public List<string> PathList= new List<string>();
        public float screenWidth;
        public float screenHeight;
        private const int mSampleSize = 12;
        Android.Content.Context mContext;

        public ImageViewer( Android.Content.Context context ) {
            mContext = context;
            DisplayMetrics dm = new DisplayMetrics();
            screenWidth = ( (AppCompatActivity)context ).Resources.DisplayMetrics.WidthPixels;
            screenHeight = ( (AppCompatActivity)context ).Resources.DisplayMetrics.HeightPixels;
        }
        public void RemoveAll() {
            PathList.Clear();
        }
        public bool Existed( string path ) {
            foreach( var p in PathList ) {
                if ( p == path ) {
                    return true;
                }
            }
            return false;
        }
        public Android.Net.Uri GetUriAt(int index) {
            return Android.Net.Uri.FromFile( new Java.IO.File( PathList[index] ) );
        }
        public void AddPath( string path ) {
            PathList.Add( path );
        }

        public Bitmap GetCompressedBitmapFromIndexToShow( int position ) {
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = false;
            options.InSampleSize = mSampleSize;
            Bitmap bitmap = BitmapFactory.DecodeFile( PathList[position], options );
            return bitmap;
        }
             
    }

    public class ImageAdapter : BaseAdapter {

        public ImageViewer mImageViewer;
        private Android.Content.Context mContext;
        public ImageAdapter( Android.Content.Context context ) {
            mImageViewer = new ImageViewer( context );
            this.mContext = context;
        }

        public override int Count => mImageViewer.PathList.Count;

        public override Java.Lang.Object GetItem( int position ) {
            return null;
        }

        public override long GetItemId( int position ) {
            return position;
        }

        public override View GetView( int position, View convertView, ViewGroup parent ) {
            ImageView img = null;

            if ( convertView == null ) {
                img = new ImageView( mContext );
                img.LayoutParameters = new GridView.LayoutParams( Convert.ToInt32( mImageViewer.screenWidth / 3 ), Convert.ToInt32( mImageViewer.screenWidth / 3.5 ) );
                img.SetScaleType( ImageView.ScaleType.CenterCrop );
                img.SetPadding( 8, 8, 8, 8 );
            } else {
                img = (ImageView)convertView;
            }
            try {
                // TODO compress and load to the stream
                img.SetImageBitmap( mImageViewer.GetCompressedBitmapFromIndexToShow( position ) );
            } catch ( System.Exception ex ) {
                Toast.MakeText( mContext, "图片添加失败\n错误信息" + ex.Message, ToastLength.Long ).Show();
            }
            return img;
        }
    }
}
