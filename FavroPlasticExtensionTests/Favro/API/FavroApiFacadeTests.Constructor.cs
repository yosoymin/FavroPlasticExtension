﻿//  Favro Plastic Extension
//  Copyright(C) 2020  David Harillo Sánchez
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
using NUnit.Framework;

namespace FavroPlasticExtension.Favro.API
{
    public partial class FavroApiFacadeTests
    {
        [TestCase(Category = "Constructor,FavroApiFacade")]
        public void Constructor_NullConnection_ShouldThrow()
        {
            //Assert:
            Assert.Throws<ArgumentNullException>(() => new FavroApiFacade(null, logMock.Object));
        }

        [TestCase(Category = "Constructor,FavroApiFacade")]
        public void Constructor_NullLogger_ShouldThrow()
        {
            // Assert:
            Assert.Throws<ArgumentNullException>(() => new FavroApiFacade(connectionMock.Object, null));
        }
    }
}
