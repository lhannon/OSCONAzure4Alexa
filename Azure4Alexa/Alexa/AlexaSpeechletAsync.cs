﻿using System;
using System.Collections.Generic;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.Slu;
using AlexaSkillsKit.UI;
using System.Diagnostics;
using AlexaSkillsKit.Authentication;
using AlexaSkillsKit.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Bot.Connector.DirectLine;
using System.Linq;
using Microsoft.ApplicationInsights.TraceListener;

namespace Azure4Alexa.Alexa
{
    // Follow the AlexaSkillsKit documentation and override the base class and children with our own implementation
    // We'll implement the web services friendly async variant - SpeechletAsync

    // the functions below map to the Alexa requests described at this URL
    // https://developer.amazon.com/public/solutions/alexa/alexa-skills-kit/docs/handling-requests-sent-by-alexa

    public class AlexaSpeechletAsync : SpeechletAsync
    {

        // Alexa provides a security wrapper around requests sent to your service, which the 
        // AlexaSkillsKit nuget package validates by default.  However, you might not want this wrapper enabled while
        // you do local development and testing - in DEBUG mode.

        // Note: the default Azure publishing option in Visual Studio is Release (not Debug), so by default the
        // security wrapper will be enabled when you publish to Azure.

        // Amazon requires that your skill validate requests sent to it for certification, so you shouldn't 
        // deploy to production with validation disabled

        //#if DEBUG

        public override bool OnRequestValidation(SpeechletRequestValidationResult result, DateTime referenceTimeUtc, SpeechletRequestEnvelope requestEnvelope)
        {
            return true;
        }

        //#endif

        private DirectLineClient dlClient;
        private static string directLineSecret = "odJ5VKzB8oY.cwA.9E0.IkVvS809oGsfqJPFTe4kui6sYWbPXqUbSTUnLncIn_U";
        private static string botId = "sftsrc";
        private string fromUser = "DirectLineSampleClientUser";
        private ChannelAccount from;

        public override Task OnSessionStartedAsync(SessionStartedRequest sessionStartedRequest, Session session)
        {
            // this function is invoked when a user begins a session with your skill
            // this is a chance to load user data at the start of a session

            // if the inbound request doesn't include your Alexa Skills AppId or you haven't updated your
            // code to include the correct AppId, return a visual and vocal error and do no more
            // Update the AppId variable in AlexaConstants.cs to resolve this issue

            if (AlexaUtils.IsRequestInvalid(session))
            {
                return Task.FromResult<SpeechletResponse>(InvalidApplicationId(session));
            }

            // to-do - up to you

            // return some sort of Task per function definition
            return Task.Delay(0);
        }

        public override Task OnSessionEndedAsync(SessionEndedRequest sessionEndedRequest, Session session)
        {
            // this function is invoked when a user ends a session with your skill
            // this is a chance to save user data at the end of a session
            
            // if the inbound request doesn't include your Alexa Skills AppId or you haven't updated your
            // code to include the correct AppId, return a visual and vocal error and do no more
            // Update the AppId variable in AlexaConstants.cs to resolve this issue

            if (AlexaUtils.IsRequestInvalid(session))
            {
                return Task.FromResult<SpeechletResponse>(InvalidApplicationId(session));
            }


            // to-do - up to you

            // return some sort of Task per function definition
            return Task.Delay(0);

        }

        public override async Task<SpeechletResponse> OnLaunchAsync(LaunchRequest launchRequest, Session session)
        {
            // this function is invoked when the user invokes your skill without an intent

            // if the inbound request doesn't include your Alexa Skills AppId or you haven't updated your
            // code to include the correct AppId, return a visual and vocal error and do no more
            // Update the AppId variable in AlexaConstants.cs to resolve this issue

            if (AlexaUtils.IsRequestInvalid(session))
            {
                return await Task.FromResult<SpeechletResponse>(InvalidApplicationId(session));
            }


            return await Task.FromResult<SpeechletResponse>(GetOnLaunchAsyncResult(session));
        }

        IDictionary<string, Conversation> conversations = new Dictionary<string, Conversation>();
        IDictionary<string, string> watermarks = new Dictionary<string, string>();

        string SendToBotFramework(string sessionId, string text)
        {
            dlClient = new DirectLineClient(directLineSecret);
            if (!conversations.ContainsKey(sessionId))
            {
                // start a new conversation
                conversations[sessionId] = dlClient.Conversations.StartConversation();
                watermarks[sessionId] = null;
            }
            else
            {
                dlClient.Conversations.ReconnectToConversation(conversations[sessionId].ConversationId,
                    watermarks[sessionId]);
            }
            Microsoft.Bot.Connector.DirectLine.Activity msg = new Microsoft.Bot.Connector.DirectLine.Activity
            {
                From = new ChannelAccount(sessionId),
                Text = text,
                Type = ActivityTypes.Message
            };

            dlClient.Conversations.PostActivity(conversations[sessionId].ConversationId, msg);
            var activitySet = dlClient.Conversations.GetActivities(conversations[sessionId].ConversationId, watermarks[sessionId]);
            watermarks[sessionId] = activitySet.Watermark;

            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;
            return activities.FirstOrDefault().Text;
        }

        public override async Task<SpeechletResponse> OnIntentAsync(IntentRequest intentRequest, Session session)
        //        public override Task<SpeechletResponse> OnIntentAsync(IntentRequest intentRequest, Session session)
        {
            Trace.TraceInformation("AlexaSpeechletAsync called");
            
            // if the inbound request doesn't include your Alexa Skills AppId or you haven't updated your
            // code to include the correct AppId, return a visual and vocal error and do no more
            // Update the AppId variable in AlexaConstants.cs to resolve this issue

            if (AlexaUtils.IsRequestInvalid(session))
            {
                Trace.TraceInformation("AlexaSpeechletAsync InvalidApplication");
                return await Task.FromResult<SpeechletResponse>(InvalidApplicationId(session));
            }

            // this function is invoked when Amazon matches what the user said to 
            // one of your defined intents.  now you will need to handle
            // the request

            // intentRequest.Intent.Name contains the name of the intent
            // intentRequest.Intent.Slots.* contains slot values if you're using them 
            // session.User.AccessToken contains the Oauth 2.0 access token if the user has linked to your auth system

            // Get intent from the request object.
            Intent intent = intentRequest.Intent;
            string intentName = (intent != null) ? intent.Name : null;

            // If there's no match between the intent passed and what we support, (i.e. you forgot to implement
            // a handler for the intent), default the user to the standard OnLaunch request

            // you'll probably be calling a web service to handle your intent
            // this is a good place to create an httpClient that can be recycled across REST API requests
            // don't be evil and create a ton of them unnecessarily, as httpClient doesn't clean up after itself

            var httpClient = new HttpClient();
            Trace.TraceInformation("AlexaSpeechletAsync called with intentName " + intentName);
            switch (intentName)
            {
                case "CatchAllIntent":
                    try
                    {
                        string resp = SendToBotFramework(session.SessionId, intentRequest.Intent.Slots["Search"].Value);

                        return await Task.FromResult<SpeechletResponse>(AlexaUtils.BuildSpeechletResponse(
                            new AlexaUtils.SimpleIntentResponse() { cardText = resp }, true)); 
                    } catch (Exception ex)
                    {
                        return await Task.FromResult<SpeechletResponse>(AlexaUtils.BuildSpeechletResponse(
                            new AlexaUtils.SimpleIntentResponse() { cardText = ex.ToString() }, true)); 

                    }

                case "AMAZON.FallbackIntent":
                    return await Task.FromResult<SpeechletResponse>(AlexaUtils.BuildSpeechletResponse(
                            new AlexaUtils.SimpleIntentResponse() { cardText = "Decider received the Fallback Intent" }, true)); 
                    
             
                // add your own intent handler

                // case ("YourCustomIntent"):
                //   return await YourCustomIntentClass(session, whateverYouNeedToPass);
                //   invalid pattern with change // return Task.FromResult<SpeechletResponse>(YourCustomIntentClass(session, whateverYouNeedToPass));

                // did you forget to implement an intent?
                // just send the user to the intent-less utterance

                default:
                    return await Task.FromResult<SpeechletResponse>(GetOnLaunchAsyncResult(session));
            }

        }


        private SpeechletResponse GetOnLaunchAsyncResult(Session session)
        {
            // called by OnLaunchAsync - when the user invokes your skill without an intent
            // called by OnIntentAsync if you forget to map an intent to an action
            Trace.TraceInformation("In GetOnLaunchAsyncResult, about to send botframework \'Hello\'");
            string resp = SendToBotFramework(session.SessionId, "hello");
            return AlexaUtils.BuildSpeechletResponse(new AlexaUtils.SimpleIntentResponse() { cardText = resp }, false);
        }


        private SpeechletResponse InvalidApplicationId(Session session)
        {
            // if the inbound request doesn't include your Alexa Skills AppId or you haven't updated your
            // code to include the correct AppId, return a visual and vocal error and do no more
            // Update the AppId variable in AlexaConstants.cs to resolve this issue

            return AlexaUtils.BuildSpeechletResponse(new AlexaUtils.SimpleIntentResponse()
            {
                cardText = "An invalid Application ID was received from Alexa.  Please update your Visual Studio project " +
                    "to include the correct value and then re-deploy your Azure project."
            }, true);
        }

        // handle the enqueuing of the next track in a multi-track album, which can occur after Alexa informs your
        // service (this service) that the current track has begun.

        public override async Task<SpeechletResponse> OnAudioPlayerAsync(AudioPlayerRequest audioPlayerRequest,
            Context context)
        {
            var httpClient = new HttpClient();
            return await Groove.Music.EnqueueGrooveMusic(context, httpClient, "ENQUEUE");
        }

        // handle the user asking for the next track in an album or playlist 
        // i.e. "Alexa, Next".  This can be expanded to the other AudioPlayer intents, such as repeat, back, etc.

        public override async Task<SpeechletResponse> OnAudioIntentAsync(AudioIntentRequest audioIntentRequest,
    Context context)
        {
            var httpClient = new HttpClient();
            if (audioIntentRequest.Intent.Name == "AMAZON.NextIntent")
            {
                return await Groove.Music.EnqueueGrooveMusic(context, httpClient, "AMAZON.NextIntent");
            }
            return null;

        }
    }
}