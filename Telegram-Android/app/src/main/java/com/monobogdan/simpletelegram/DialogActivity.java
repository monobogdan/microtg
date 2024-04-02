package com.monobogdan.simpletelegram;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.Dialog;
import android.content.ClipboardManager;
import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.PowerManager;
import android.util.DisplayMetrics;
import android.view.ContextMenu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.animation.AlphaAnimation;
import android.widget.BaseAdapter;
import android.widget.EditText;
import android.widget.ListView;
import android.widget.PopupMenu;
import android.widget.PopupWindow;
import android.widget.TextView;

import com.monobogdan.simpletelegram.transport.ClientManager;
import com.monobogdan.simpletelegram.transport.Packets;
import com.monobogdan.simpletelegram.utils.SoundManager;

import java.util.List;

public class DialogActivity extends Activity {

    private class MessagesAdapter extends BaseAdapter {
        private List<Packets.Message> messages;

        public void setMessages(List<Packets.Message> messages) {
            this.messages = messages;
            notifyDataSetChanged();
        }

        public boolean isDatasetDiffers(List<Packets.Message> messages) {
            if(messages != null && this.messages != null && messages.size() > 0 && this.messages.size() > 0) {
                return messages.get(0).ID != this.messages.get(0).ID;
            }

            return true;
        }

        @Override
        public int getCount() {
            return messages != null ? messages.size() : 0;
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

            //view.setAnimation(alphaAnim);
        }

        private void setMessageData(ViewGroup view, Packets.Message message) {
            ((TextView)view.findViewById(R.id.message_content)).setText(message.Text);
            //((TextView)view.findViewById(R.id.message_date)).setText(message.Date);

            view.setTag(message);
        }

        private void attachEventHandler(final View view) {
            view.setLongClickable(true);
            view.setOnLongClickListener(new View.OnLongClickListener() {
                @Override
                public boolean onLongClick(View view) {
                    view.showContextMenu();

                    return true;
                }
            });

            view.setOnCreateContextMenuListener(new View.OnCreateContextMenuListener() {
                @Override
                public void onCreateContextMenu(ContextMenu contextMenu, final View view, ContextMenu.ContextMenuInfo contextMenuInfo) {
                    // Reply to...
                    contextMenu.add(getString(R.string.reply)).setOnMenuItemClickListener(new MenuItem.OnMenuItemClickListener() {
                        @Override
                        public boolean onMenuItemClick(MenuItem menuItem) {
                            setReplyContext((Packets.Message) view.getTag());

                            return true;
                        }
                    });

                    // Copy
                    contextMenu.add(getString(R.string.copy)).setOnMenuItemClickListener(new MenuItem.OnMenuItemClickListener() {
                        @Override
                        public boolean onMenuItemClick(MenuItem menuItem) {
                            ViewGroup vg = (ViewGroup)view;

                            android.text.ClipboardManager manager = (android.text.ClipboardManager) view.getContext().getSystemService(CLIPBOARD_SERVICE);
                            manager.setText(((TextView)vg.findViewById(R.id.message_content)).getText());

                            return true;
                        }
                    });

                    // Send to...
                    contextMenu.add(getString(R.string.resend)).setOnMenuItemClickListener(new MenuItem.OnMenuItemClickListener() {
                        @Override
                        public boolean onMenuItemClick(MenuItem menuItem) {
                            ViewGroup vg = (ViewGroup)view;
                            String text = ((TextView)vg.findViewById(R.id.message_content)).getText().toString();

                            Intent intent = new Intent();
                            intent.setAction(Intent.ACTION_SEND);
                            intent.putExtra(Intent.EXTRA_TEXT, text);
                            intent.setType("text/plain");
                            startActivity(Intent.createChooser(intent, null));

                            return true;
                        }
                    });
                }
            });
        }

        @Override
        public View getView(int idx, View view, ViewGroup viewGroup) {
            if(view == null) {
                view = getLayoutInflater().inflate(R.layout.message_item, null, false);

                beginFadeOutAnimation(view, 350 + (idx * 200));
                attachEventHandler(view);
            }

            setMessageData((ViewGroup) view, messages.get(idx));

            return view;
        }
    }

    private Packets.Chat chat;
    private Packets.Message replyContext;
    private MessagesAdapter adapter;

    private PowerManager pm;

    private void setReplyContext(Packets.Message message) {
        ViewGroup group = (ViewGroup) findViewById(R.id.reply_message);
        group.setVisibility(message != null ? View.VISIBLE : View.GONE);

        if(message != null) {
            ((TextView)group.findViewById(R.id.reply_preview)).setText(message.Text);
        }

        replyContext = message;
    }

    private void updateMessages() {
        ClientManager.getCurrent().queryMessages(chat, 50, new ClientManager.Response() {
            @Override
            public void onReady(final String str) {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        try {
                            List<Packets.Message> messages = Packets.parseMessageListFromQueryResponse(str);

                            if(adapter == null) {
                                adapter = new MessagesAdapter();
                                ((ListView)findViewById(R.id.messages_view)).setAdapter(adapter);
                            }

                            if(adapter.isDatasetDiffers(messages))
                                adapter.setMessages(messages);
                        } catch (Exception e) {
                            e.printStackTrace();
                        }
                    }
                });
            }
        });
    }

    public void onSend(View sender) {
        EditText editText = ((EditText)findViewById(R.id.message_text));

        if(editText.getText().length() > 0) {
            long replyTo = replyContext != null ? replyContext.ID : 0;

            ClientManager.getCurrent().sendTextMessage(chat.ID, editText.getText().toString(), replyTo, new ClientManager.Response() {
                @Override
                public void onReady(String str) {
                    SoundManager.getInstance().start(R.raw.message_sent);
                }
            });

            editText.setText("");
            setReplyContext(null);
        }
    }

    public void onViewChatInfo(View sender) {
        Dialog dialog = new Dialog(this, R.style.DialogTheme);
        dialog.setContentView(R.layout.user_dialog);
        dialog.show();
    }

    private void updateHeader() {
        ((TextView)findViewById(R.id.dialog_name)).setText(chat.Name);
    }

    @Override
    public void onCreateContextMenu(ContextMenu menu, View v, ContextMenu.ContextMenuInfo menuInfo) {
        super.onCreateContextMenu(menu, v, menuInfo);


    }

    @Override
    public void onBackPressed() {
        finish();
    }

    private void setupUpdateHandler() {
        final Handler handler = new Handler();
        handler.post(new Runnable() {
            @Override
            public void run() {
                updateMessages();

                handler.postDelayed(this, 5000);
            }
        });
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);


        pm = (PowerManager) getSystemService(POWER_SERVICE);

        // Get chat details
        chat = new Packets.Chat();
        Intent intent = getIntent();
        chat.ID = intent.getLongExtra("id", 0);
        chat.Name = intent.getStringExtra("name");

        setContentView(R.layout.dialog_activity);

        setupUpdateHandler();
        updateHeader();
    }
}
