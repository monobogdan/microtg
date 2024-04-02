package com.monobogdan.simpletelegram.utils;

import android.content.Context;
import android.media.AudioManager;
import android.media.SoundPool;
import android.util.Log;

import com.monobogdan.simpletelegram.R;

import java.util.Dictionary;
import java.util.Hashtable;

public class SoundManager {
    private static final String TAG = "SoundManager";
    private static final int MAX_STREAMS = 8;

    private static SoundManager instance;

    private SoundPool pool;
    private Context context;
    private Dictionary<Integer, Integer> dict;

    public static void initialize(Context ctx) {
        if(instance == null)
            instance = new SoundManager(ctx);
    }

    public static SoundManager getInstance() {
        return instance;
    }

    private void precache(int id) {
        dict.put(id, pool.load(context, id, 0));
    }

    private SoundManager(Context ctx) {
        context = ctx;
        pool = new SoundPool(MAX_STREAMS, AudioManager.STREAM_NOTIFICATION, 0);
        dict = new Hashtable<>();

        Log.i(TAG, "precache: Precaching sound effects");

        precache(R.raw.message_sent);
    }

    public void start(int id) {
        pool.play(dict.get(id), 1, 1, 1, 0, 1.0f);
    }
}
