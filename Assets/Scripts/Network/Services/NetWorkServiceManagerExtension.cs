using System.Collections.Generic;
using UnityEngine;

///全部注释掉，是因为发送网络请求的配置已经迁移到 BaseRequestParam 及其子类中去了，理论上不存在直接调用以下接口的情况了

public partial class NetworkServiceManager
{
    // /// <summary>
    // /// 发送GET请求
    // /// </summary>
    // public string Get(string endpoint, Transform lockable, ResponseEvent callback = null, Dictionary<string, string> queryParams = null)
    // {
    //     var requestParams = new BaseRequestParam
    //     {
    //         url = BuildUrl(endpoint),
    //         method = "GET",
    //         queryParams = queryParams ?? new Dictionary<string, string>()
    //     };
    //
    //     return SendRequest(requestParams, lockable, callback);
    // }
    //
    // /// <summary>
    // /// 发送POST请求
    // /// </summary>
    // public string Post(string endpoint, object requestData, Transform lockable = null, ResponseEvent callback = null)
    // {
    //     var requestParams = new BaseRequestParam
    //     {
    //         url = BuildUrl(endpoint),
    //         method = "POST",
    //         requestData = requestData
    //     };
    //
    //     return SendRequest(requestParams, lockable, callback);
    // }
    //
    // /// <summary>
    // /// 发送 FormData 请求（支持多种数据类型）
    // /// </summary>
    // public string SendFormData(string url, List<FormField> formFields, 
    //     string method = "POST", Transform lockable = null,
    //     ResponseEvent callback = null)
    // {
    //     var requestParams = new BaseRequestParam
    //     {
    //         url = url,
    //         method = method,
    //         FormDataFields = formFields
    //     };
    //     
    //     return SendRequest(requestParams, lockable, callback);
    // }
    
    // /// <summary>
    // /// 发送文件上传请求
    // /// </summary>
    // public string UploadFile(string url, string fieldName, byte[] fileData, 
    //                         string fileName, Dictionary<string, string> additionalFields = null,
    //                         Transform lockable = null, NetworkServiceManager.ResponseEvent callback = null)
    // {
    //     var formFields = new List<FormField>();
    //     
    //     // 添加文件
    //     formFields.Add(FormField.CreateFile(fieldName, fileData, fileName));
    //     
    //     // 添加额外字段
    //     if (additionalFields != null)
    //     {
    //         foreach (var kvp in additionalFields)
    //         {
    //             formFields.Add(FormField.CreateText(kvp.Key, kvp.Value));
    //         }
    //     }
    //     
    //     return SendFormData(url, formFields, "POST", lockable, callback);
    // }
    //
    // /// <summary>
    // /// 发送图片上传请求
    // /// </summary>
    // public string UploadImage(string url, string fieldName, byte[] imageData,
    //                          string fileName = "image.jpg", 
    //                          Dictionary<string, string> additionalFields = null,
    //                          Transform lockable = null, NetworkServiceManager.ResponseEvent callback = null)
    // {
    //     var formFields = new List<FormField>();
    //     
    //     // 添加图片文件
    //     formFields.Add(FormField.CreateFile(fieldName, imageData, fileName, "image/jpeg"));
    //     
    //     // 添加额外字段
    //     if (additionalFields != null)
    //     {
    //         foreach (var kvp in additionalFields)
    //         {
    //             formFields.Add(FormField.CreateText(kvp.Key, kvp.Value));
    //         }
    //     }
    //     
    //     return SendFormData(url, formFields, "POST", lockable, callback);
    // }
    //
    // /// <summary>
    // /// 发送PUT请求
    // /// </summary>
    // public string Put(string endpoint, object requestData, Transform lockable = null, NetworkServiceManager.ResponseEvent callback = null)
    // {
    //     var requestParams = new BaseRequestParam
    //     {
    //         url = BuildUrl(endpoint),
    //         method = "PUT",
    //         requestData = requestData
    //     };
    //
    //     return SendRequest(requestParams, lockable, callback);
    // }
    //
    // /// <summary>
    // /// 发送DELETE请求
    // /// </summary>
    // public string Delete(string endpoint, Transform lockable = null, NetworkServiceManager.ResponseEvent callback = null)
    // {
    //     var requestParams = new BaseRequestParam
    //     {
    //         url = BuildUrl(endpoint),
    //         method = "DELETE"
    //     };
    //
    //     return SendRequest(requestParams, lockable, callback);
    // }
}