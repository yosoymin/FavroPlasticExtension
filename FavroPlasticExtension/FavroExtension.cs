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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Codice.Utils;
using FavroPlasticExtension.Favro;
using FavroPlasticExtension.Favro.API;
using log4net;

namespace Codice.Client.IssueTracker.FavroExtension
{
    public class FavroExtension : IPlasticIssueTrackerExtension
    {
        internal const string KEY_USER = "User";
        internal const string KEY_PASSWORD = "Password";
        internal const string KEY_ORGANIZATION = "OrganizationId";
        internal const string KEY_TODO_COLUMN = "TODO column name";
        internal const string KEY_DOING_COLUMN = "Doing column name";
        internal const string KEY_COLLECTION_ID = "CollectionId";
        internal const string KEY_WIDGET_ID = "WidgetCommonId";
        internal const string KEY_BRANCH_PREFIX = "Prefix";
        internal const string KEY_BRANCH_SUFFIX = "Suffix";
        internal const string COMMENT_TEMPLATE = "repository: {0}\nCheckin ID: {1}\nGUID: {2}\n\n{3}";

        private const string EXTENSION_NAME = "Favro extension";
        private readonly IssueTrackerConfiguration configuration;
        private readonly ILog logger;
        private IFavroConnection connection;
        private ApiFacade apiMethods;
        private Organization organizationInfo;
        private string organizationShortName;
        private Dictionary<string, User> usersCache;
        private Dictionary<string, List<Column>> columnsCache = new Dictionary<string, List<Column>>();

        internal FavroExtension(IssueTrackerConfiguration configuration, ILog logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            logger.Info("Favro issue tracker is initialized");
        }

        #region IPlasticIssueTrackerExtension implementation
        public string GetExtensionName()
        {
            return EXTENSION_NAME;
        }

        private void CheckExistsUsersCache()
        {
            if (usersCache == null)
            {
                usersCache = new Dictionary<string, User>();
                var users = apiMethods.GetAllUsers();
                foreach (var user in users)
                    usersCache[user.UserId] = user;
            }
        }

        private void CheckExistsColumnsCache(string widgetCommonId)
        {
            if (!columnsCache.ContainsKey(widgetCommonId))
                columnsCache[widgetCommonId] = apiMethods.GetAllColumns(widgetCommonId);
        }

        private Column FindColumn(Card card)
        {
            if (card.WidgetCommonId != null)
            {
                CheckExistsColumnsCache(card.WidgetCommonId);
                return columnsCache[card.WidgetCommonId].Find(column => column.ColumnId == card.ColumnId);
            }
            else
                return null;
        }

        private Column FindColumn(string widgetCommonId, string name)
        {
            CheckExistsColumnsCache(widgetCommonId);
            return columnsCache[widgetCommonId].Find(column => column.Name == name);
        }

        public void Connect()
        {
            connection = CreateConnection(configuration);
            apiMethods = new ApiFacade(connection, logger);
        }

        public void Disconnect()
        {
            connection = null;
            apiMethods = null;
        }

        public bool TestConnection(IssueTrackerConfiguration configuration)
        {
            var testConnection = CreateConnection(configuration);
            var testMethods = new ApiFacade(testConnection, logger);
            try
            {
                return testMethods.GetOrganization(testConnection.OrganizationId) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
        {
            string comment = CreateComment(changeset);
            foreach (var task in tasks)
            {
                var card = GetCardFromSequentialId(GetCardSequentialIdFromTaskId(task.Id));
                apiMethods.CreateComment(comment, card.CardCommonId);
            }
        }

        private string CreateComment(PlasticChangeset changeset)
        {
            return string.Format(COMMENT_TEMPLATE, changeset.Repository, changeset.Id, changeset.Guid, changeset.Comment);
        }

        public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
        {
            throw new NotImplementedException();
        }

        public PlasticTask GetTaskForBranch(string fullBranchName)
        {
            var branchName = GetBranchName(fullBranchName);
            var cardId = GetCardSequentialIdFromBranchName(branchName);
            return GetTaskFromCardSequentialId(cardId);
        }

        public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
        {
            Dictionary<string, PlasticTask> branchesTasks = new Dictionary<string, PlasticTask>();
            foreach (var fullBranchName in fullBranchNames)
            {
                var task = GetTaskForBranch(fullBranchName);
                if (task != null)
                {
                    branchesTasks.Add(fullBranchName, task);
                }
            }
            return branchesTasks;
        }

        public void OpenTaskExternally(string taskId)
        {
            Process.Start(GetExternalLink(GetCardSequentialIdFromTaskId(taskId)));
        }

        public List<PlasticTask> LoadTasks(List<string> taskIds)
        {
            var result = new List<PlasticTask>();
            foreach (var taskId in taskIds)
            {
                var task = GetTaskFromCardSequentialId(GetCardSequentialIdFromTaskId(taskId));
                if (task != null)
                {
                    result.Add(task);
                }
            }
            return result;
        }

        public List<PlasticTask> GetPendingTasks()
        {
            var pendingCards = apiMethods.GetAssignedCards(GetCollectionId(), GetWidgetCommonId());
            var todoColumnName = configuration.GetValue(KEY_TODO_COLUMN);
            return pendingCards.Select(_ => ConvertToTask(_)).Where(task => task.Status == todoColumnName).ToList();
        }

        public List<PlasticTask> GetPendingTasks(string assignee)
        {
            var tasks = GetPendingTasks();
            return tasks.Where(task => task.Owner == assignee).ToList();
        }

        public void MarkTaskAsOpen(string taskId, string assignee)
        {
            // TODO: podria ser interesante asignar usuario a la tarjeta en caso de que no lo este previamente
            var cardSequentialId = GetCardSequentialIdFromTaskId(taskId);
            var card = GetCardFromSequentialId(cardSequentialId);
            var doingColumnName = configuration.GetValue(KEY_DOING_COLUMN);
            apiMethods.MoveCardToColumn(card, FindColumn(card.WidgetCommonId, doingColumnName));
        }
        #endregion

        private string GetDecryptedPassword(string encryptedPassword)
        {
            return CryptoServices.GetDecryptedPassword(encryptedPassword);
        }

        private IFavroConnection CreateConnection(IssueTrackerConfiguration connectionConfiguration)
        {
            var user = connectionConfiguration.GetValue(KEY_USER);
            var password = connectionConfiguration.GetValue(KEY_PASSWORD);
            var connection = new Connection(user, GetDecryptedPassword(password));
            connection.OrganizationId = connectionConfiguration.GetValue(KEY_ORGANIZATION);
            return connection;
        }

        private string GetOrganizationShortName()
        {
            if (organizationInfo == null)
                organizationInfo = apiMethods.GetOrganization(connection.OrganizationId);

            if (organizationShortName == null)
            { 
                var organizationName = organizationInfo.Name.Replace(" ", "");
                organizationShortName = organizationName.Substring(0, 3);
            }

            return organizationShortName;
        }

        private string GetBranchName(string fullBranchName)
        {
            var lastSeparatorIndex = fullBranchName.LastIndexOf('/');
            var branchName = string.Empty;
            if (lastSeparatorIndex < 0)
            {
                branchName = fullBranchName;
            }
            else if (lastSeparatorIndex < (fullBranchName.Length - 1))
            {
                branchName = fullBranchName.Substring(lastSeparatorIndex + 1);
            }
            return branchName;
        }

        private string GetCardSequentialIdFromTaskId(string taskId)
        {
            var suffix = Regex.Escape(GetSuffix());
            var regex = new Regex($"(\\d+){suffix}");
            var match = regex.Match(taskId);
            string cardId = null;
            if (match.Success)
            {
                cardId = match.Groups[1].Value;
            }
            return cardId;
        }

        private string GetCardSequentialIdFromBranchName(string branchName)
        {
            var prefix = Regex.Escape(GetPrefix());
            var suffix = Regex.Escape(GetSuffix());
            var regex = new Regex($"{prefix}(\\d+){suffix}");
            var match = regex.Match(branchName);
            string cardId = null;
            if (match.Success)
            {
                cardId = match.Groups[1].Value;
            }
            return cardId;
        }

        private string GetPrefix()
        {
            return configuration.GetValue(KEY_BRANCH_PREFIX);
        }

        private string GetSuffix()
        {
            return configuration.GetValue(KEY_BRANCH_SUFFIX);
        }

        private string GetCollectionId()
        {
            return configuration.GetValue(KEY_COLLECTION_ID);
        }

        private string GetWidgetCommonId()
        {
            return configuration.GetValue(KEY_WIDGET_ID);
        }

        private Card GetCardFromSequentialId(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId) && int.TryParse(cardId, out int cardSequentialId))
                return apiMethods.GetCard(cardSequentialId);
            else
                return null;
        }

        private PlasticTask GetTaskFromCardSequentialId(string cardId)
        {
            var card = GetCardFromSequentialId(cardId);
            if (card != null)
                return ConvertToTask(card);
            else
                return null;
        }

        private PlasticTask ConvertToTask(Card card)
        {
            PlasticTask result = null;
            if (card != null)
            {
                string userMail = "";
                string status = "unknown";
                // Get first user assigned who has not completed the task
                foreach (var assign in card.Assignments)
                {
                    if (assign.Completed == false)
                    {
                        CheckExistsUsersCache();
                        userMail = usersCache[assign.UserId].Email;
                        var column = FindColumn(card);
                        if (column != null)
                            status = column.Name;
                        break;
                    }
                }
                result = new PlasticTask
                {
                    Description = GetDescription(card),
                    Title = card.Name,
                    Owner = userMail,
                    Id = card.SequentialId.ToString() + GetSuffix(),
                    Status = status
                };
            }
            return result;
        }

        private string GetDescription(Card card)
        {
            var cardLink = GetExternalLink(card);
            return $"{card.Name}: {cardLink}";
        }

        private string GetExternalLink(Card card)
        {
            return GetExternalLink(card.SequentialId.ToString());
        }

        private string GetExternalLink(string cardId)
        {
            var shortName = GetOrganizationShortName();
            return $"https://favro.com/organization/{connection.OrganizationId}/?card={shortName}-{cardId}";
        }
    }
}
