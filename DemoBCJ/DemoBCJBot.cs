// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DemoBCJ.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace DemoBCJ
{
    /// <summary>
    ///     Represents a bot that processes incoming activities.
    ///     For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    ///     This is a Transient lifetime service. Transient lifetime services are created
    ///     each time they're requested. Objects that are expensive to construct, or have a lifetime
    ///     beyond a single turn, should be carefully managed.
    ///     For example, the <see cref="MemoryStorage" /> object and associated
    ///     <see cref="IStatePropertyAccessor{T}" /> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1" />
    public class DemoBCJBot : IBot
    {
        public static readonly string LuisKey = "BCJTest";

        // Services configured from the ".bot" file.
        private readonly BotServices _services;

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public DemoBCJBot(BotServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            if (!_services.LuisServices.ContainsKey(DemoBCJBot.LuisKey))
            {
                throw new ArgumentException("Invalid configuration....");
            }
        }

        /// <summary>
        ///     Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">
        ///     A <see cref="ITurnContext" /> containing all the data needed
        ///     for processing this conversation turn.
        /// </param>
        /// <param name="cancellationToken">
        ///     (Optional) A <see cref="CancellationToken" /> that can be used by other objects
        ///     or threads to receive notice of cancellation.
        /// </param>
        /// <returns>A <see cref="Task" /> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet" />
        /// <seealso cref="ConversationState" />
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                try
                {
                    // Check LUIS model
                    var recognizerResult = await _services.LuisServices[LuisKey]
                                                          .RecognizeAsync(turnContext, cancellationToken);

                    var topIntent = recognizerResult?.GetTopScoringIntent();

                    if (topIntent != null
                        && topIntent.HasValue
                        && topIntent.Value.intent != "None")
                    {
                        await turnContext.SendActivityAsync($"==>LUIS Top Scoring Intent: {topIntent.Value.intent}, Score: {topIntent.Value.score}\n");
                    }
                    else
                    {
                        var msg = @"No LUIS intents were found.
                        This sample is about identifying two user intents:
                        'Calendar.Add'
                        'Calendar.Find'
                        Try typing 'Add Event' or 'Show me tomorrow'.";
                        await turnContext.SendActivityAsync(msg);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                await turnContext.SendActivityAsync($"HELLO!");
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }
        }
    }
}
