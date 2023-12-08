using Azure;
using Azure.AI.OpenAI;

namespace WhisperTranslate.API;

public class TranslationService
{
    readonly OpenAIClient _client;

    public TranslationService()
    {
        _client = new OpenAIClient(new Uri("[Your Azure OAI endpoint]"), 
            new AzureKeyCredential("[Your key]"));
    }

    public async Task<string> Translate(BinaryData audio, string to)
    {
        string translation;

        if (to.Equals("english", StringComparison.InvariantCultureIgnoreCase))
        {
            translation = await TranslateToEnglish(audio);
        }
        else
        {
            var transcription = await Transcribe(audio);
            translation = await Translate(transcription, to);
        }

        return translation;
    }


    private async Task<string> Transcribe(BinaryData data)
    {
        var response = await _client.GetAudioTranscriptionAsync(
            new AudioTranscriptionOptions("whisper", data));

        return response.Value.Text;
    }

    private async Task<string> TranslateToEnglish(BinaryData data)
    {
        var response = await _client.GetAudioTranslationAsync(
            new AudioTranslationOptions("whisper", data));

        return response.Value.Text;
    }

    private async Task<string> Translate(string content, string to)
    {
        var chatResult = await _client.GetChatCompletionsAsync(
            new ChatCompletionsOptions
            {
                DeploymentName = "gpt-35-turbo",
                Temperature = 0,
                Messages =
                {
                    new(ChatRole.System, $"You are a translator, translate the text entered by the user to {to}."),
                    new ChatMessage(ChatRole.User, content),
                }
            });

        var answer = chatResult.Value.Choices[0].Message.Content;
        return answer;
    }
}
