﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ContextExtensions;
using Microsoft.Bot.Builder.Middleware;
using Microsoft.Bot.Schema;

namespace RyanDawkins.Typist.Middleware
{
    /// <summary>
    /// This typist middleware makes it appear as if the bot was a person typing by delaying the messages sent sequentially.
    /// </summary>
    public class TypistMiddleware : ISendActivity
    {

        private readonly int _typistWordsPerMinute;
        const int SECONDS_PER_MINUTE = 60;
        const int MILISECONDS_PER_SECOND = 1000;

        public TypistMiddleware(int typistWordsPerMinute)
        {
            _typistWordsPerMinute = typistWordsPerMinute;
        }

        public async Task SendActivity(IBotContext context, IList<IActivity> activities, MiddlewareSet.NextDelegate next)
        {
            List<IMessageActivity> messageActivities = activities
                .Where(activity => activity.Type.Equals(ActivityTypes.Message))
                .Select(activity => activity.AsMessageActivity())
                .ToList();
            if (!messageActivities.Any())
            {
                await next();
                return;
            }

            foreach (IMessageActivity activity in messageActivities)
            {
                string[] words = activity.Text.Split((char[])null);
                double timeInMinutes = ((double)words.Count() / _typistWordsPerMinute);
                int timeInMs = (int)Math.Ceiling(timeInMinutes * SECONDS_PER_MINUTE * MILISECONDS_PER_SECOND) / 2;

                for (int i = 0; i < (timeInMs / 2000); i++)
                {
                    // this keeps show typing visible for the duration of our delays.
                    context.ShowTyping();
                    await context.Delay(2000);
                }

                // Prevent the activity from being pushed through the pipe after the middleware chain
                activities.Remove(activity);

                // Push the activity now
                await context.Bot.Adapter.Send(new List<IActivity>() { activity });
            }

            await next();
        }
    }
}