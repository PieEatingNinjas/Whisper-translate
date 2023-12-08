using Plugin.Maui.Audio;
using System.Net.Http.Headers;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace WhisperTranslate.Mobile;

public partial class MainPage : ContentPage
{
    IAudioManager audioManager;
    IAudioRecorder? audioRecorder;
    byte[]? audioBytes;

    public MainPage()
    {
        audioManager = AudioManager.Current;

        InitializeComponent();
    }

    private async void Start_Clicked(object sender, EventArgs e)
    {
        audioBytes = null;
        btnStopRec.IsVisible = true;
        btnStartRec.IsVisible = false;
        lblStatus.Text = "Recording...";

        try
        {
            await StartRecording();
        }
        catch (PermissionException pe)
        {
            lblStatus.Text = pe.Message;
        }

    }

    private async Task StartRecording()
    {
        if (await CheckPermissionIsGrantedAsync<Microphone>())
        {
            audioRecorder = audioManager.CreateRecorder();

            await audioRecorder.StartAsync();
        }
        else
        {
            throw new PermissionException("No permission granted to access Microphone");
        }
    }

    private async void Stop_Clicked(object sender, EventArgs e)
    {
        await StopRecording();

        lblStatus.Text = "Recorded";

        btnTranslate.IsEnabled = true;

        btnTranslate.IsVisible = true;
        btnStartRec.IsVisible = true;
        btnStopRec.IsVisible = false;
    }

    private async Task StopRecording()
    {
        if (audioRecorder is not null)
        {
            var audioSource = await audioRecorder.StopAsync();

            var audioStream = audioSource.GetAudioStream();

            List<byte> totalStream = new();
            byte[] buffer = new byte[32];
            int read;

            while ((read = audioStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalStream.AddRange(buffer.Take(read));
            }

            audioBytes = totalStream.ToArray();
        }
    }

    private async void Translate_Clicked(object sender, EventArgs e)
    {
        if (audioBytes is not null)
        {
            lblStatus.Text = "Translating...";
            var result = await TranslateAudio(audioBytes, Picker.SelectedItem as string ?? Picker.Items.First());
            lblResult.Text = result;
            lblStatus.Text = "Translated";
        }
    }

    private async Task<string> TranslateAudio(byte[] bytes, string to)
    {
        using (var httpClient = new HttpClient())
        {
            using (var form = new MultipartFormDataContent())
            {
                using (var fileContent = new ByteArrayContent(bytes))
                {
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    form.Add(fileContent, "file", "audio");
                    HttpResponseMessage response = await httpClient.PostAsync($"https://localhost:7224/translate?to={to}", form);

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
    }

    public static async Task<bool> CheckPermissionIsGrantedAsync<TPermission>() where TPermission : BasePermission, new()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<TPermission>();

        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // Prompt the user to turn on in settings
            // On iOS once a permission has been denied it may not be requested again from the application
            return false;
        }

        if (Permissions.ShouldShowRationale<TPermission>())
        {
            // Prompt the user with additional information as to why the permission is needed
        }

        status = await Permissions.RequestAsync<TPermission>();

        return status == PermissionStatus.Granted;
    }
}