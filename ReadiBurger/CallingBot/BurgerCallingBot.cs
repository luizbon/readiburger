using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Calling;
using Microsoft.Bot.Builder.Calling.Events;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;

namespace ReadiBurger.CallingBot
{
    public class BurgerCallingBot : ICallingBot
    {
        public ICallingBotService CallingBotService { get; }

        private readonly List<string> _response = new List<string>();
        private int _silenceTimes;
        private bool _sttFailed;

        public BurgerCallingBot(ICallingBotService callingBotService)
        {
            CallingBotService = callingBotService ?? throw new ArgumentNullException(nameof(callingBotService));

            CallingBotService.OnIncomingCallReceived += OnIncomingCallReceived;
            CallingBotService.OnPlayPromptCompleted += OnPlayPromptCompleted;
            CallingBotService.OnRecordCompleted += OnRecordCompleted;
            CallingBotService.OnHangupCompleted += OnHangupCompleted;
            CallingBotService.OnRecognizeCompleted += OnRecognizeCompleted;
        }

        public void Dispose()
        {
            if (CallingBotService == null) return;

            CallingBotService.OnIncomingCallReceived -= OnIncomingCallReceived;
            CallingBotService.OnPlayPromptCompleted -= OnPlayPromptCompleted;
            CallingBotService.OnRecordCompleted -= OnRecordCompleted;
            CallingBotService.OnHangupCompleted -= OnHangupCompleted;
            CallingBotService.OnRecognizeCompleted -= OnRecognizeCompleted;
        }

        private static Task OnIncomingCallReceived(IncomingCallEvent incomingCallEvent)
        {
            var id = Guid.NewGuid().ToString();
            incomingCallEvent.ResultingWorkflow.Actions = new List<ActionBase>
            {
                new Answer { OperationId = id },
                GetRecordForText("Welcome to ReadiBurger, what would you like today?")
            };

            return Task.FromResult(true);
        }

        private static ActionBase GetRecordForText(string promptText)
        {
            var prompt = string.IsNullOrEmpty(promptText) ? null : GetPromptForText(promptText);
            var id = Guid.NewGuid().ToString();
            return new Record
            {
                OperationId = id,
                PlayPrompt = prompt,
                MaxDurationInSeconds = 10,
                InitialSilenceTimeoutInSeconds = 5,
                MaxSilenceTimeoutInSeconds = 1,
                PlayBeep = false,
                RecordingFormat = RecordingFormat.Wav,
                StopTones = new List<char> { '#' }
            };
        }
        private Task OnPlayPromptCompleted(PlayPromptOutcomeEvent playPromptOutcomeEvent)
        {
            if (_response.Count > 0)
            {
                _silenceTimes = 0;
                var actionList = new List<ActionBase> { GetPromptForText(_response), GetRecordForText(string.Empty) };

                playPromptOutcomeEvent.ResultingWorkflow.Actions = actionList;
                _response.Clear();
            }
            else
            {
                if (_sttFailed)
                {
                    playPromptOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetRecordForText("I didn't catch that, would you kindly repeat?")
                    };
                    _sttFailed = false;
                    _silenceTimes = 0;
                }
                else if (_silenceTimes > 2)
                {
                    playPromptOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetPromptForText("Something went wrong. Call again later."),
                        new Hangup { OperationId = Guid.NewGuid().ToString() }
                    };
                    playPromptOutcomeEvent.ResultingWorkflow.Links = null;
                    _silenceTimes = 0;
                }
                else
                {
                    _silenceTimes++;
                    playPromptOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetSilencePrompt(2000)
                    };
                }
            }
            return Task.CompletedTask;
        }

        private async Task OnRecordCompleted(RecordOutcomeEvent recordOutcomeEvent)
        {
            if (recordOutcomeEvent.RecordOutcome.Outcome == Outcome.Success)
            {
                var record = await recordOutcomeEvent.RecordedContent;
                var bs = new BingSpeech(recordOutcomeEvent.ConversationResult, (s, b) =>
                {
                    // Don't know why message is sent twice, so I'm preventing from speaking it two times
                    if (_response.All(x => x != s))
                        _response.Add(s);
                }, s => _sttFailed = s);
                bs.CreateDataRecoClient();
                bs.SendAudioHelper(record);
                recordOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                {
                    GetSilencePrompt()
                };
            }
            else
            {
                if (_silenceTimes > 1)
                {
                    recordOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetPromptForText("Thank you for calling"),
                        new Hangup { OperationId = Guid.NewGuid().ToString() }
                    };
                    recordOutcomeEvent.ResultingWorkflow.Links = null;
                    _silenceTimes = 0;
                }
                else
                {
                    _silenceTimes++;
                    recordOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetRecordForText("I didn't catch that, would you kindly repeat?")
                    };
                }
            }
        }

        private async Task OnRecognizeCompleted(RecognizeOutcomeEvent recognizeOutcomeEvent)
        {
            if (recognizeOutcomeEvent.RecognizeOutcome.Outcome == Outcome.Success)
            {
                var record = recognizeOutcomeEvent.RecognizeOutcome.CollectDigitsOutcome;
                var bs = new BingSpeech(recognizeOutcomeEvent.ConversationResult, (s, b) => _response.Add(s), s => _sttFailed = s);
                await bs.SendToBot(record.Digits);
                recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                {
                    GetSilencePrompt()
                };
            }
            else
            {
                if (_silenceTimes > 1)
                {
                    recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetPromptForText("Thank you for calling"),
                        new Hangup { OperationId = Guid.NewGuid().ToString() }
                    };
                    recognizeOutcomeEvent.ResultingWorkflow.Links = null;
                    _silenceTimes = 0;
                }
                else
                {
                    _silenceTimes++;
                    recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetPromptForText("I didn't catch that, would you kindly repeat?")
                    };
                }
            }
        }

        private static Task OnHangupCompleted(HangupOutcomeEvent hangupOutcomeEvent)
        {
            hangupOutcomeEvent.ResultingWorkflow = null;
            return Task.FromResult(true);
        }

        private static PlayPrompt GetPromptForText(string text)
        {
            return new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = new List<Prompt> { GetPrompt(text) } };
        }

        private static PlayPrompt GetPromptForText(IEnumerable<string> text)
        {
            var prompts = text.Where(txt => !string.IsNullOrEmpty(txt))
                .Select(txt => GetPrompt(txt)).ToList();
            return prompts.Count == 0 ? GetSilencePrompt(1000) : new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = prompts };
        }

        private static PlayPrompt GetSilencePrompt(uint silenceLengthInMilliseconds = 3000)
        {
            return new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = new List<Prompt> { GetPrompt(string.Empty, silenceLengthInMilliseconds) } };
        }

        private static Prompt GetPrompt(string value, uint? silience = null)
        {
            return new Prompt
            {
                Value = value,
                Voice = VoiceGender.Female,
                SilenceLengthInMilliseconds = silience
            };
        }
    }
}