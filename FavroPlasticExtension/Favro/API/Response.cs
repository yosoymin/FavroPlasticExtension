﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FavroPlasticExtension.Favro.API
{
    /// <summary>
    /// Stores the information of an API response
    /// </summary>
    internal class Response
    {
        /// <summary>
        /// The maximum number of request permitted to make per hour
        /// </summary>
        public const string HEADER_LIMIT_MAX = "X-RateLimit-Limit";
        /// <summary>
        /// The number of request remaining in the current rate limit window
        /// </summary>
        public const string HEADER_LIMIT_REMAINING = "X-RateLimit-Remaining";
        /// <summary>
        /// The time at which the current rate limit window resets in UTC
        /// </summary>
        public const string HEADER_LIMIT_RESET = "X-RateLimit-Reset";
        /// <summary>
        /// The backend server that returned this response
        /// </summary>
        public const string HEADER_BACKEND_ID = "X-Favro-Backend-Identifier";
        public const string PATH_REQUEST_ID = "requestId";
        public const string PATH_ENTRIES = "entries";
        public const string PATH_PAGE = "page";
        public const string PATH_PAGES = "pages";
        public const string PATH_LIMIT = "limit";
        /// <summary>
        /// The special headers of the Favro API that where detected in the response
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
        /// <summary>
        /// Content of the response
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// If some error happened, stores the response exception.
        /// </summary>
        public Exception Error { get; set; }

        private JObject deserializedContent;

        public Response()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the current page of the responses that use pagination. If this response
        /// has no pages.
        /// </summary>
        /// <returns>Page of information stored in this response object</returns>
        /// <exception cref="InvalidOperationException">If this response has no pages</exception>
        public int GetPageNumber()
        {
            if (Error != null || string.IsNullOrWhiteSpace(Content))
            {
                throw new InvalidOperationException("No page number available");
            }
            return ExtractPageNumberFromContent();
        }

        private int ExtractPageNumberFromContent()
        {
            try
            {
                var page = GetCurrentPage();
                if (page <= 0)
                {
                    throw new ArgumentException("Expected value over 0");
                }
                return page;
            }
            catch (Exception e) when (!(e is ArgumentException))
            {
                throw new InvalidOperationException("Unable to extract page from response content", e);
            }
        }

        private int GetCurrentPage()
        {
            var data = GetDeserializedData();
            return data.GetValue(PATH_PAGE).Value<int>();
        }


        private JObject GetDeserializedData()
        {
            if (deserializedContent == null)
            {
                deserializedContent = JsonConvert.DeserializeObject<JObject>(Content);
            }
            return deserializedContent;
        }

        /// <summary>
        /// Indicates if there are more pages of information about the resources retrieved and
        /// stored in this response object.
        /// </summary>
        /// <returns><c>true</c> if there are more pages to be retrieved from the server;
        /// otherwise <c>false</c></returns>
        public bool HasMorePages()
        {
            var result = false;
            if (Error == null)
            {
                try
                {
                    var currentPage = GetCurrentPage();
                    var numPages = GetNumPages();
                    result = currentPage < numPages;
                }
                catch (Exception)
                {
                    // Ignore the error
                }
            }
            return result;
        }

        private int GetNumPages()
        {
            var data = GetDeserializedData();
            return data.GetValue(PATH_PAGES).Value<int>();
        }
    }
}
