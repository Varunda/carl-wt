﻿using System;

namespace watchtower.Code.ExtensionMethods {

    public static class DateTimeExtensionMethods {

        public static string GetDiscordTimestamp(this DateTime when, string format) {
            return $"<t:{new DateTimeOffset(when).ToUnixTimeSeconds()}:{format}>";
        }

        public static string GetDiscordFullTimestamp(this DateTime when) {
            return GetDiscordTimestamp(when, "f");
        }

        public static string GetDiscordRelativeTimestamp(this DateTime when) {
            return GetDiscordTimestamp(when, "R");
        }

        public static bool Between(this DateTime when, DateTime rangeStart, DateTime rangeEnd) {
            return when >= rangeStart && when < rangeEnd;
        }

    }
}
