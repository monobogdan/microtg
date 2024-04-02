package com.monobogdan.simpletelegram;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.AlphaAnimation;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import com.monobogdan.simpletelegram.transport.ClientManager;
import com.monobogdan.simpletelegram.transport.Packets;

import java.util.List;

public class MainActivity extends Activity {

    private class DialogAdapter extends BaseAdapter {
        private List<Packets.Chat> chats;

        public void setChats(List<Packets.Chat> chats) {
            this.chats = chats;
            notifyDataSetChanged();
        }

        @Override
        public int getCount() {
            return chats.size();
        }

        @Override
        public Object getItem(int i) {
            return null;
        }

        @Override
        public long getItemId(int i) {
            return 0;
        }

        private void beginFadeOutAnimation(View view, long duration) {
            AlphaAnimation alphaAnim = new AlphaAnimation(0, 1);
            alphaAnim.setDuration(duration);
            alphaAnim.start();

            view.setAnimation(alphaAnim);
        }

        private void setChatData(ViewGroup view, Packets.Chat chat) {
            ((TextView)view.findViewById(R.id.dialog_sender)).setText(chat.Name);
            ((TextView)view.findViewById(R.id.dialog_message)).setText(chat.Text);

            if(chat.Avatar != null)
                ((ImageView)view.findViewById(R.id.dialog_avatar_preview)).setImageBitmap(chat.Avatar);

            view.setTag(chat);
        }

        private void attachHandler(ViewGroup view) {
            view.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View view) {
                    openDialog((Packets.Chat) view.getTag());
                }
            });
        }

        @Override
        public View getView(int idx, View view, ViewGroup viewGroup) {
            if(view == null) {
                view = getLayoutInflater().inflate(R.layout.dialog_item, null, false);

                beginFadeOutAnimation(view, 350 + (idx * 200));
                attachHandler((ViewGroup) view);
            }

            Packets.Chat associatedChat = chats.get(idx);
            setChatData((ViewGroup) view, associatedChat);

            return view;
        }
    }

    public void openDialog(Packets.Chat chat) {
        Intent intent = new Intent();
        intent.setClass(this, DialogActivity.class);
        intent.putExtra("id", chat.ID);
        intent.putExtra("name", chat.Name);

        startActivity(intent);
    }

    private void updateDialogList() {
        ClientManager.getCurrent().queryChats(50, new ClientManager.Response() {
            @Override
            public void onReady(final String str) {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        try {
                            List<Packets.Chat> chats = Packets.parseChatListFromQueryResponse(str);

                            for (final Packets.Chat chat :
                                    chats) {
                                if(chat.Photo.length() == 0)
                                    continue;

                                PhotoCache.getInstance().scheduleDownload(ClientManager.getCurrent().getPhotoUrl(chat.Photo), new PhotoCache.Response() {
                                    @Override
                                    public void onReady(Bitmap bmp) {
                                        chat.Avatar = bmp;
                                    }

                                    @Override
                                    public void onFailed() {

                                    }
                                });
                            }

                            DialogAdapter adapter = new DialogAdapter();
                            adapter.setChats(chats);

                            ((ListView) findViewById(R.id.messages_view)).setAdapter(adapter);
                        } catch (Exception e) {
                            e.printStackTrace();
                            Toast.makeText(MainActivity.this, "Не сработало!", Toast.LENGTH_LONG).show();
                        }
                    }
                });
            }
        });
    }

    private void prepareClientManager() {
        Configuration.ApplicationConfiguration appConfiguration = Configuration.getInstance().getApplicationConfiguration();;

        ClientManager.getCurrent().setNodeAddress(appConfiguration.nodeAddress);
        ClientManager.getCurrent().setAuthorizationToken(appConfiguration.sessionKey);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        PhotoCache.initialize(getApplicationContext().getCacheDir().getAbsolutePath());

        ClientManager.getCurrent().setErrorHandler(new ClientManager.NetworkErrorHandler() {
            @Override
            public void onError(String str, int code) {
                Toast.makeText(getApplicationContext(),
                        String.format("%s: %s", getApplicationContext().getString(R.string.networkError), str),
                        Toast.LENGTH_SHORT).show();
            }
        });

        ClientManager.getCurrent().setNodeAddress("http://192.168.0.111:13377");
        setContentView(R.layout.dialog_list_activity);

        updateDialogList();
    }
}
