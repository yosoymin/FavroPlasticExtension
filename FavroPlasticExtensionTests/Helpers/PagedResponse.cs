﻿//  Favro Plastic Extension
//  Copyright(C) 2019  David Harillo Sánchez
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published
//  by the Free Software Foundation, either version 3 of the License, or
//  any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details in the project root.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program. If not, see<https://www.gnu.org/licenses/>

using System.Collections.Generic;

namespace FavroPlasticExtensionTests.Helpers
{
    public class PagedResponse<TEntry>
    {
        /// <summary>
        /// Identifer of the request
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// Index of the page
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// Number of pages
        /// </summary>
        public int Pages { get; set; }
        /// <summary>
        /// Limit of entries in the list
        /// </summary>
        public int Limit { get; set; }
        /// <summary>
        /// List of entries returned in this page
        /// </summary>
        public List<TEntry> Entries { get; set; }
    }
}
