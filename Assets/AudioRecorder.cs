using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Google.Cloud.Speech.V1;
using Google.Protobuf;
using TMPro;
using Google.Api.Gax.Grpc;
using System.Runtime.InteropServices;
using System;

public class AudioRecorder : MonoBehaviour
{
    private AudioClip _recordedClip;
    private bool _isRecording = false;
    private string _microphoneDevice;
    public TextMeshProUGUI resultText;
    private const int sampleRate = 16000;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            _microphoneDevice = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("마이크를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            StartRecording();
        }

        if (Input.GetKeyUp(KeyCode.O))
        {
            StopRecording();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(ProcessRecording());
        }
    }

    void StartRecording()
    {
        if (!_isRecording && _microphoneDevice != null)
        {
            _recordedClip = Microphone.Start(_microphoneDevice, false, 10, sampleRate);
            _isRecording = true;
            Debug.Log("녹음을 시작합니다.");
        }
    }

    void StopRecording()
    {
        if (_isRecording)
        {
            Microphone.End(_microphoneDevice);
            _isRecording = false;
            Debug.Log("녹음을 중지합니다.");
        }
    }

    IEnumerator ProcessRecording()
    {
        if (_recordedClip != null)
        {
            float[] samples = new float[_recordedClip.samples];
            _recordedClip.GetData(samples, 0);
            byte[] audioData = ConvertAudioClipToPCM16(samples, _recordedClip.channels);
            yield return StartCoroutine(RecognizeSpeech(audioData));
        }
        else
        {
            Debug.LogWarning("녹음된 음성이 없습니다.");
        }
    }

    byte[] ConvertAudioClipToPCM16(float[] samples, int channels)
    {
        MemoryStream memoryStream = new MemoryStream();
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(samples[i] * short.MaxValue);
            memoryStream.WriteByte((byte)(sample & 0x00ff));
            memoryStream.WriteByte((byte)((sample & 0xff00) >> 8));
        }
        return memoryStream.ToArray();
    }

    IEnumerator RecognizeSpeech(byte[] audioData)
    {
        string apiKeyPath = Path.Combine(Application.streamingAssetsPath, "project.json");
        var grpcAdapter = GrpcCoreAdapter.Instance;  // 명시적으로 GrpcCoreAdapter 사용
        var speech = new SpeechClientBuilder
        {
            CredentialsPath = apiKeyPath,
            GrpcAdapter = grpcAdapter  // GrpcAdapter 설정
        }.Build();

        var response = speech.Recognize(new RecognitionConfig
        {
            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
            SampleRateHertz = sampleRate,
            LanguageCode = "en"
        }, RecognitionAudio.FromBytes(audioData));

        yield return null;

        if (response.Results.Count > 0)
        {
            resultText.text = response.Results[0].Alternatives[0].Transcript;
        }
        else
        {
            resultText.text = "음성을 인식할 수 없습니다.";
        }
    }
}