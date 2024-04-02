package com.monobogdan.simpletelegram.transport;

import android.graphics.Bitmap;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.StringReader;
import java.lang.reflect.Field;
import java.net.URLDecoder;
import java.net.URLEncoder;
import java.util.ArrayList;
import java.util.List;

public class Packets {

    public static final class KeyValuePair {
        public String Key;
        public String Value;

        public KeyValuePair(String key, String value) {
            Key = key;
            Value = value;
        }
    }

    public static final class Chat {
        public Bitmap Avatar; // Not serialized

        public long ID;
        public long Date;
        public String Name;
        public String Text;
        public long MsgId;
        public String Photo;
    }

    public static final class Message {
        public long ID;
        public long Date;
        public long Sender;
        public String Text;
    }

    public final class User {

    }

    private static KeyValuePair parseKeyValuePair(String line) {
        if(!line.contains("="))
            throw new RuntimeException("Failed to parse key-value pair " + line);

        return new KeyValuePair(line.substring(0, line.indexOf('=')), line.substring(line.indexOf('=') + 1));
    }

    private static void assignFieldFromPair(Object obj, KeyValuePair pair) {
        try {
            Field field = obj.getClass().getField(pair.Key);

            if(field.getType() == int.class) {
                field.set(obj, Integer.parseInt(pair.Value));
            }
            if(field.getType() == long.class) {
                field.set(obj, Long.parseLong(pair.Value));
            }
            if(field.getType() == String.class) {
                field.set(obj, URLDecoder.decode(pair.Value));
            }
        } catch (Exception e) {
            e.printStackTrace(); // To maintain compatibility with older client versions while we add support for new fields.
        }
    }

    public static List<Chat> parseChatListFromQueryResponse(String resp) throws IOException {
        BufferedReader reader = new BufferedReader(new StringReader(resp));
        KeyValuePair lineCount = parseKeyValuePair(reader.readLine());

        if(!lineCount.Key.equals("Count")) {
            throw new IllegalArgumentException("First line in response should be \"Count\"");
        }

        List<Chat> ret = new ArrayList<>();
        String str = "";

        Chat dataSetObj = new Chat();

        while((str = reader.readLine()) != null) {
            if(str.equals("Begin")) {
                // Begin dataset object
                dataSetObj = new Chat();
                continue;
            }
            if(str.equals("End")) {
                ret.add(dataSetObj);
                continue;
            }

            if(str.length() < 1 || str.charAt(0) == '#')
                continue; // Comments are allowed

            KeyValuePair pair = parseKeyValuePair(str);
            assignFieldFromPair(dataSetObj, pair);
        }

        return ret;
    }

    public static List<Message> parseMessageListFromQueryResponse(String resp) throws IOException {
        BufferedReader reader = new BufferedReader(new StringReader(resp));
        KeyValuePair lineCount = parseKeyValuePair(reader.readLine());

        if(!lineCount.Key.equals("Count")) {
            throw new IllegalArgumentException("First line in response should be \"Count\"");
        }

        List<Message> ret = new ArrayList<>();
        String str = "";

        Message dataSetObj = new Message();

        while((str = reader.readLine()) != null) {
            if(str.equals("Begin")) {
                // Begin dataset object
                dataSetObj = new Message();
                continue;
            }
            if(str.equals("End")) {
                ret.add(dataSetObj);
                continue;
            }

            if(str.length() < 1 || str.charAt(0) == '#')
                continue; // Comments are allowed

            KeyValuePair pair = parseKeyValuePair(str);
            assignFieldFromPair(dataSetObj, pair);
        }

        return ret;
    }

    public static boolean isErrorneousResponse(String response) {
        return false;
    }
}
