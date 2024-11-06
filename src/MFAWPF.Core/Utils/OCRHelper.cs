using MFAWPF.Core.Models;
using MFAWPF.Core.Services;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using Newtonsoft.Json;
using Avalonia.Media.Imaging;
using System.IO;

namespace MFAWPF.Core.Utils;

public class OCRHelper
{
    public class RecognitionQuery
    {
        [JsonProperty("all")] public List<RecognitionResult>? All;
        [JsonProperty("best")] public RecognitionResult? Best;
        [JsonProperty("filtered")] public List<RecognitionResult>? Filtered;
    }

    public class RecognitionResult
    {
        [JsonProperty("box")] public List<int>? Box;
        [JsonProperty("score")] public double? Score;
        [JsonProperty("text")] public string? Text;
    }

    public static void Initialize()
    {
    }

    public static string ReadTextFromMAATasker(int x, int y, int width, int height)
    {
        string result = string.Empty;
        var taskItemViewModel = new TaskItemViewModel
        {
            Task = new TaskModel
            {
                Recognition = "OCR",
                Roi = new List<int> { x, y, width, height }
            },
            Name = "AppendOCR",
        };

        var job = MaaProcessor.Instance?.GetCurrentTasker()?
            .AppendPipeline(taskItemViewModel.Name, taskItemViewModel.ToString());

        if (job?.Wait() == MaaJobStatus.Succeeded)
        {
            var query = JsonConvert.DeserializeObject<RecognitionQuery>(
                job.QueryRecognitionDetail()?.Detail ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(query?.Best?.Text))
                result = query.Best.Text;
        }
        else
        {
            NotificationService.Error("识别失败！");
        }

        LoggerService.LogInfo($"识别结果: {result}");
        return result;
    }

    public static string ReadTextFromMAAContext(IMaaContext context, IMaaImageBuffer image, int x, int y,
        int width, int height)
    {
        var result = string.Empty;
        var taskItemViewModel = new TaskItemViewModel
        {
            Task = new TaskModel
            {
                Recognition = "OCR",
                Roi = new List<int> { x, y, width, height }
            },
            Name = "AppendOCR",
        };

        var detail = context.RunRecognition(taskItemViewModel.Name, taskItemViewModel.ToString(), image);

        if (detail != null)
        {
            var query = JsonConvert.DeserializeObject<RecognitionQuery>(detail.Detail);
            if (!string.IsNullOrWhiteSpace(query?.Best?.Text))
                result = query.Best.Text;
        }
        else
        {
            NotificationService.Error("识别失败！");
        }

        LoggerService.LogInfo($"识别结果: {result}");
        return result;
    }

    // Avalonia 版本的图像处理方法
    public static string ReadTextFromBitmap(Bitmap bitmap, int x, int y, int width, int height)
    {
        string result = string.Empty;
        try
        {
            using var croppedBitmap = new CroppedBitmap(bitmap, new Avalonia.PixelRect(x, y, width, height));
            using var memoryStream = new MemoryStream();
            croppedBitmap.Save(memoryStream);

            // TODO: 实现 OCR 处理逻辑
            // 这里需要根据具体使用的 OCR 引擎来实现

            return result;
        }
        catch (Exception ex)
        {
            LoggerService.LogError(ex);
            NotificationService.Error($"图像处理失败：{ex.Message}");
            return string.Empty;
        }
    }
} 