package com.monobogdan.simpletelegram;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStreamWriter;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class PhotoCache {
    public interface Response {
        void onReady(Bitmap bmp);
        void onFailed();
    }

    private static PhotoCache instance;

    private String path;
    private ExecutorService imageThreadPool;

    public static void initialize(String path) {
        instance = new PhotoCache(path);
    }

    public static PhotoCache getInstance() {
        return instance;
    }

    private PhotoCache(String path) {
        this.path = path;

        imageThreadPool = Executors.newFixedThreadPool(2);
    }

    public void scheduleDownload(final String url, final Response resp) {
        imageThreadPool.execute(new Runnable() {
            @Override
            public void run() {
                try
                {
                    String fileName = url.substring(url.lastIndexOf('/') + 1);
                    String cacheName = String.format("%s/%s", path, fileName);

                    File cachedPreview = new File(cacheName);
                    Bitmap bmp = null;

                    if(cachedPreview.exists())
                    {
                        bmp = BitmapFactory.decodeFile(cachedPreview.getAbsolutePath());

                        if(bmp != null) {
                            resp.onReady(bmp);

                            return; // Else, if bitmap is corrupt, we proceed to download new cached file
                        }
                    }
                    else {
                        HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
                        conn.setDoInput(true);
                        conn.setRequestMethod("GET");
                        conn.setRequestProperty("User-Agent", "Mozilla/4.0 (compatible; MSIE 4.01; Windows NT)");
                        conn.connect();

                        InputStream reader = conn.getInputStream();
                        byte[] buf = new byte[conn.getContentLength()];

                        int ptr = 0;
                        while (ptr < buf.length) {
                            ptr += reader.read(buf, ptr, buf.length - ptr);
                        }
                        bmp = BitmapFactory.decodeByteArray(buf, 0, buf.length);

                        FileOutputStream foStream = new FileOutputStream(cachedPreview);
                        foStream.write(buf);
                    }

                    if(bmp != null) {
                        resp.onReady(bmp);
                    }
                    else
                    {
                        resp.onFailed();
                    }
                }
                catch (Exception e)
                {
                    e.printStackTrace();

                    resp.onFailed();
                }
            }
        });
    }
}
