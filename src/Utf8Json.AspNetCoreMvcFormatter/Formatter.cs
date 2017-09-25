﻿using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;

namespace Utf8Json.AspNetCoreMvcFormatter
{
    public class MessagePackOutputFormatter : IOutputFormatter //, IApiResponseTypeMetadataProvider
    {
        const string ContentType = "application/json";
        static readonly string[] SupportedContentTypes = new[] { ContentType };

        readonly IJsonFormatterResolver resolver;

        public MessagePackOutputFormatter()
            : this(null)
        {

        }
        public MessagePackOutputFormatter(IJsonFormatterResolver resolver)
        {
            this.resolver = resolver ?? JsonSerializer.DefaultResolver;
        }

        //public IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        //{
        //    return SupportedContentTypes;
        //}

        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return true;
        }

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            context.HttpContext.Response.ContentType = ContentType;

            // when 'object' use the concrete type(object.GetType())
            if (context.ObjectType == typeof(object))
            {
                JsonSerializer.NonGeneric.Serialize(context.HttpContext.Response.Body, context.Object, resolver);
                return Task.CompletedTask;
            }
            else
            {
                JsonSerializer.NonGeneric.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object, resolver);
                return Task.CompletedTask;
            }
        }
    }

    public class MessagePackInputFormatter : IInputFormatter // , IApiRequestFormatMetadataProvider
    {
        const string ContentType = "application/json";
        static readonly string[] SupportedContentTypes = new[] { ContentType };

        readonly IJsonFormatterResolver resolver;

        public MessagePackInputFormatter()
            : this(null)
        {

        }

        public MessagePackInputFormatter(IJsonFormatterResolver resolver)
        {
            this.resolver = resolver ?? JsonSerializer.DefaultResolver;
        }

        //public IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        //{
        //    return SupportedContentTypes;
        //}

        public bool CanRead(InputFormatterContext context)
        {
            return true;
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = JsonSerializer.NonGeneric.Deserialize(context.ModelType, request.Body, resolver);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}