using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeOffBot.Utils
{
    public class CardHelper
    {
        public static ThumbnailCard MakeRequestCard(string subTitle, string text)
        {
            var thumbnailCard = new ThumbnailCard
            {
                Title = "Time-off Request",
                Subtitle = subTitle,
                Text = text,
                Images = new List<CardImage> { new CardImage("http://freedesignfile.com/upload/2014/04/Summer-beach-vacation-background-art-vector-01.jpg") }
            };

            return thumbnailCard;
        }
    }
}