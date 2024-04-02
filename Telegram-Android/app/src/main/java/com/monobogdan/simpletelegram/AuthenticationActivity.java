package com.monobogdan.simpletelegram;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.ProgressDialog;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;

import com.monobogdan.simpletelegram.transport.ClientManager;
import com.monobogdan.simpletelegram.utils.SoundManager;

import java.util.Locale;

public class AuthenticationActivity extends Activity {

    private boolean checkAuthState() {
        return false;
    }

    private void openDialogListActivity() {
        // Prepare ClientManager state

        Intent intent = new Intent();
        intent.setClass(getApplicationContext(), MainActivity.class);
        startActivity(intent);

        finish();
    }

    public void onAuthorize(View sender) {
        final String node = ((EditText)findViewById(R.id.server)).getText().toString();
        final String authKey = ((EditText)findViewById(R.id.auth_key)).getText().toString();

        if(node.length() < 1 || authKey.length() < 1) {
            Toast.makeText(getApplicationContext(), R.string.fillFields, Toast.LENGTH_LONG).show();

            return;
        }

        if(!node.contains(":")) {
            Toast.makeText(getApplicationContext(), R.string.nodeValidation, Toast.LENGTH_LONG).show();

            return;
        }

        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage(R.string.connecting);
        builder.setCancelable(false);
        final AlertDialog dlg = builder.show();

        ClientManager.getCurrent().setErrorHandler(new ClientManager.NetworkErrorHandler() {
            @Override
            public void onError(String str, int code) {
                dlg.cancel();

                Toast.makeText(AuthenticationActivity.this, str, Toast.LENGTH_SHORT).show();
            }
        });

        ClientManager.getCurrent().checkCredentialsValidity(node, authKey, new ClientManager.CredentialsResponse() {
            @Override
            public void onReady(final boolean isValid) {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        dlg.cancel();

                        if(isValid) {
                            Configuration.ApplicationConfiguration appCfg = Configuration.getInstance().getApplicationConfiguration();

                            appCfg.sessionKey = authKey;
                            appCfg.nodeAddress = node;
                            Configuration.getInstance().save();

                            openDialogListActivity();
                        }
                        else {
                            Toast.makeText(AuthenticationActivity.this, R.string.accessKeyMismatch, Toast.LENGTH_SHORT).show();
                        }
                    }
                });
            }
        });
    }

    public void onViewHelp(View sender) {

    }

    // TODO: Move to other place. Right now AuthActivity is used because it's first activity to load.
    private void initializeStaticResources() {
        SoundManager.initialize(getApplicationContext());
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        initializeStaticResources();

        Configuration.initialize(getApplicationContext());

        if(Configuration.getInstance().isAuthorized()) {
            openDialogListActivity();

            return;
        }

        setContentView(R.layout.auth_activity);
    }
}
