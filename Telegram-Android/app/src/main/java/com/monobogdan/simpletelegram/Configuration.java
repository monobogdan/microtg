package com.monobogdan.simpletelegram;

import android.content.Context;
import android.content.SharedPreferences;

public class Configuration {
    public class ApplicationConfiguration {
        public String sessionKey;
        public String nodeAddress;

        public void load(SharedPreferences prefs) {
            sessionKey = prefs.getString("auth_key", null);
            nodeAddress = prefs.getString("node_address", null);
        }

        public void save(SharedPreferences prefs) {
            SharedPreferences.Editor edit = prefs.edit();

            edit.putString("auth_key", sessionKey);
            edit.putString("node_address", nodeAddress);
            edit.commit();
        }
    }

    private static String SHARED_PREFS_KEY = "configuration";
    private static Configuration instance;

    public static Configuration getInstance() {
        return instance;
    }

    public static void initialize(Context ctx) {
        instance = new Configuration(ctx);
    }

    private Context context;
    private SharedPreferences sharedPrefs;

    private ApplicationConfiguration applicationConfiguration;

    private Configuration(Context ctx) {
        context = ctx;

        sharedPrefs = context.getSharedPreferences(SHARED_PREFS_KEY, Context.MODE_PRIVATE);
        applicationConfiguration = new ApplicationConfiguration();
    }

    public ApplicationConfiguration getApplicationConfiguration() {
        return applicationConfiguration;
    }

    public boolean isAuthorized() {
        return sharedPrefs.contains("auth_key") && sharedPrefs.contains("node_address");
    }

    public void load() {
        if(isAuthorized())
            applicationConfiguration.load(sharedPrefs);
    }

    public void save() {
        applicationConfiguration.save(sharedPrefs);
    }
}
