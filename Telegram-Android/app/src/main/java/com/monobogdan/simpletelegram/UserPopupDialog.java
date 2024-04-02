package com.monobogdan.simpletelegram;

import android.content.Context;
import android.util.DisplayMetrics;
import android.view.Display;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.PopupWindow;

public class UserPopupDialog extends PopupWindow {
    private View anchorView;
    private DisplayMetrics metrics;

    public UserPopupDialog(Context ctx, View anchorView, LayoutInflater inflater, DisplayMetrics metrics) {
        super(ctx);

        this.anchorView = anchorView;

        setWidth(metrics.widthPixels - 25);
        setHeight(metrics.heightPixels - 25);
        

        this.metrics = metrics;

        setContentView(inflater.inflate(R.layout.user_dialog, null));
    }

    public void show() {
        showAtLocation(anchorView, Gravity.NO_GRAVITY, metrics.widthPixels / 2 - (getWidth() / 2), metrics.heightPixels / 2 - (getHeight() / 2));
    }
}
