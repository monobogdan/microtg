package com.monobogdan.simpletelegram.transport;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class ClientManager {
    public interface CredentialsResponse {
        void onReady(boolean isValid);
    }

    public interface Response {
        void onReady(String str);
    }

    public interface NetworkErrorHandler {
        void onError(String str, int code);
    }

    private static ClientManager instance;

    private static final String USER_AGENT = "SimpleTelegram/Android";

    private String token;
    private String nodeAddress;
    private NetworkErrorHandler errorHandler;
    private ExecutorService executorService;

    public static ClientManager getCurrent()
    {
        return (instance == null) ? (instance = new ClientManager()) : instance;
    }

    private ClientManager() {
        executorService = Executors.newFixedThreadPool(1); // Adjust in future
    }

    public void setAuthorizationToken(String token) {
        this.token = token;
    }

    public void setNodeAddress(String nodeAddress) {
        this.nodeAddress = nodeAddress;
    }

    public void setErrorHandler(NetworkErrorHandler errorHandler) {
        this.errorHandler = errorHandler;
    }

    public void sendRequest(final String req, final Response resp) {
        if(req == null || req.length() < 1)
            throw new IllegalArgumentException("Request can't be null");

        executorService.submit(new Runnable() {
            @Override
            public void run() {
                try {
                    HttpURLConnection conn = (HttpURLConnection) new URL(req).openConnection();
                    conn.setDoInput(true);
                    conn.setRequestMethod("GET");
                    conn.setRequestProperty("User-Agent", USER_AGENT);
                    conn.connect();

                    BufferedReader reader = new BufferedReader(new InputStreamReader(conn.getInputStream(), "UTF-8"));
                    String response = "";
                    String line = "";
                    while ((line = reader.readLine()) != null)
                        response += line + "\n";

                    String finalResponse = response;
                    resp.onReady(finalResponse);
                } catch (Exception e) {
                    e.printStackTrace();

                    errorHandler.onError(e.getLocalizedMessage(), 0);
                }
            }
        });
    }

    public void checkCredentialsValidity(String node, String accessKey, final CredentialsResponse resp) {
        sendRequest(String.format("http://%s/CheckCredentials?auth_key=%s", node, accessKey), new Response() {
            @Override
            public void onReady(String str) {
                resp.onReady(str.equals("OK\n"));
            }
        });
    }

    public void queryChats(int count, Response resp) {
        sendRequest(String.format("%s/QueryChats?count=%d&auth_key=%s", nodeAddress, count, token), resp);
    }

    public void queryMessages(Packets.Chat chat, long count, Response resp) {
        sendRequest(String.format("%s/QueryMessages?chat_id=%d&last_message_id=%d&count=%d&auth_key=%s", nodeAddress, chat.ID, chat.MsgId, count, token), resp);
    }

    public void sendTextMessage(long chat, String msg, long replyTo, Response resp) {
        sendRequest(String.format("%s/SendMessage?chat_id=%d&text=%s&reply_to=%d&auth_key=%s", nodeAddress, chat, URLEncoder.encode(msg), replyTo, token), resp);
    }

    public String getPhotoUrl(String photo) {
        return String.format("%s/photos/%s", nodeAddress, photo);
    }

    public boolean isResponseSucceeded(String response) {
        return response.contains("OK");
    }
}
