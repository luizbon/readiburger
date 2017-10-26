using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.CognitiveServices.SpeechRecognition;
using ReadiBurger.Dialogs;
using Conversation = Microsoft.Bot.Builder.Dialogs.Conversation;

namespace ReadiBurger.CallingBot
{
    public class BingSpeech
    {
        private readonly Action<string, bool> _callback;
        private readonly ConversationResult _conversationResult;
        private readonly Action<bool> _failedCallback;
        private DataRecognitionClient _dataClient;

        public BingSpeech(ConversationResult conversationResult, Action<string, bool> callback,
            Action<bool> failedCallback)
        {
            _conversationResult = conversationResult;
            _callback = callback;
            _failedCallback = failedCallback;
        }

        public string DefaultLocale { get; } = "en-US";
        public string BingSpeechKey { get; } = "0d908bb021d541ff85b80f0e80670bb8";

        public void CreateDataRecoClient()
        {
            _dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                SpeechRecognitionMode.ShortPhrase, DefaultLocale, BingSpeechKey);

            _dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;
        }

        public void SendAudioHelper(Stream recordedStream)
        {
            var buffer = new byte[1024];
            try
            {
                int bytesRead;
                do
                {
                    bytesRead = recordedStream.Read(buffer, 0, buffer.Length);

                    _dataClient.SendAudio(buffer, bytesRead);
                } while (bytesRead > 0);
            }
            catch (Exception ex)
            {
                // log error
            }
            finally
            {
                _dataClient.EndAudio();
            }
        }

        private async void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.RecognitionSuccess)
            {
                var mostAccuratePhrase = e.PhraseResponse.Results
                    .OrderByDescending(k => k.Confidence)
                    .FirstOrDefault();

                await SendToBot(mostAccuratePhrase?.DisplayText);
            }
            else
            {
                _failedCallback(true);
            }
        }

        public async Task SendToBot(string text)
        {
            var activity = new Activity
            {
                From = new ChannelAccount {Id = _conversationResult.Id},
                Conversation = new ConversationAccount {Id = _conversationResult.Id},
                Recipient = new ChannelAccount {Id = "Bot1"},
                ServiceUrl = "https://skype.botframework.com",
                ChannelId = "skype",
                Text = text
            };

            using (var scope = Conversation
                .Container.BeginLifetimeScope(DialogModule.LifetimeScopeTag, Configure))
            {
                scope.Resolve<IMessageActivity>
                    (TypedParameter.From((IMessageActivity) activity));
                DialogModule_MakeRoot.Register
                    (scope, () => new LuisBurgerDialog(BurgerDialog.BuildForm));
                var postToBot = scope.Resolve<IPostToBot>();
                await postToBot.PostAsync(activity, CancellationToken.None);
            }
        }

        private void Configure(ContainerBuilder builder)
        {
            builder.Register(c => new BotToUserSpeech(c.Resolve<IMessageActivity>(), _callback))
                .As<IBotToUser>()
                .InstancePerLifetimeScope();
        }
    }
}