﻿// <copyright file="MapPathMiddlewareTests.cs" company="Microsoft Open Technologies, Inc.">
// Copyright 2011-2013 Microsoft Open Technologies, Inc. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin.Builder;
using Owin;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.Owin.Mapping.Tests
{
    using AppFunc = Func<IOwinContext, Task>;
    using MsAppFunc = Func<IOwinContext, Task>;

    public class MapPathMiddlewareTests
    {
        private static readonly Action<IAppBuilder> ActionNotImplemented = new Action<IAppBuilder>(_ => { throw new NotImplementedException(); });

        private static async Task Success(IOwinContext context)
        {
            context.Response.StatusCode = 200;
            context.Set("test.PathBase", context.Request.PathBase);
            context.Set("test.Path", context.Request.Path);
        }

        private static void UseSuccess(IAppBuilder app)
        {
            app.Use(Success);
        }

        private static async Task NotImplemented(IOwinContext context)
        {
            throw new NotImplementedException();
        }

        private static void UseNotImplemented(IAppBuilder app)
        {
            app.Use(NotImplemented);
        }

        [Fact]
        public void NullArguments_ArgumentNullException()
        {
            var builder = new AppBuilder();
            var noMiddleware = new AppBuilder().Build<OwinMiddleware>();
            Assert.Throws<ArgumentNullException>(() => builder.Map(null, ActionNotImplemented));
            Assert.Throws<ArgumentNullException>(() => builder.Map("/foo", (Action<IAppBuilder>)null));
            Assert.Throws<ArgumentNullException>(() => new MapMiddleware(null, "/foo", noMiddleware));
            Assert.Throws<ArgumentNullException>(() => new MapMiddleware(noMiddleware, "/foo", null));
            Assert.Throws<ArgumentNullException>(() => new MapMiddleware(noMiddleware, null, noMiddleware));
        }

        [Theory]
        [InlineData("/foo", "", "/foo")]
        [InlineData("/foo", "", "/foo/")]
        [InlineData("/foo", "/Bar", "/foo")]
        [InlineData("/foo", "/Bar", "/foo/cho")]
        [InlineData("/foo", "/Bar", "/foo/cho/")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
        public void PathMatchFunc_BranchTaken(string matchPath, string basePath, string requestPath)
        {
            IOwinContext context = CreateRequest(basePath, requestPath);
            IAppBuilder builder = new AppBuilder();
            builder.Map(matchPath, UseSuccess);
            var app = builder.Build<OwinMiddleware>();
            app.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase);
            Assert.Equal(requestPath, context.Request.Path);
        }

        [Theory]
        [InlineData("/foo", "", "/foo")]
        [InlineData("/foo", "", "/foo/")]
        [InlineData("/foo", "/Bar", "/foo")]
        [InlineData("/foo", "/Bar", "/foo/cho")]
        [InlineData("/foo", "/Bar", "/foo/cho/")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
        public void PathMatchAction_BranchTaken(string matchPath, string basePath, string requestPath)
        {
            IOwinContext context = CreateRequest(basePath, requestPath);
            IAppBuilder builder = new AppBuilder();
            builder.Map(matchPath, subBuilder => subBuilder.Use(Success));
            var app = builder.Build<OwinMiddleware>();
            app.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath + matchPath, context.Get<string>("test.PathBase"));
            Assert.Equal(requestPath.Substring(matchPath.Length), context.Get<string>("test.Path"));
        }

        [Theory]
        [InlineData("/foo/", "", "/foo")]
        [InlineData("/foo/", "", "/foo/")]
        [InlineData("/foo/", "/Bar", "/foo")]
        [InlineData("/foo/", "/Bar", "/foo/cho")]
        [InlineData("/foo/", "/Bar", "/foo/cho/")]
        [InlineData("/foo/cho/", "/Bar", "/foo/cho")]
        [InlineData("/foo/cho/", "/Bar", "/foo/cho/do")]
        public void MatchPathHasTrailingSlash_Trimmed(string matchPath, string basePath, string requestPath)
        {
            IOwinContext context = CreateRequest(basePath, requestPath);
            IAppBuilder builder = new AppBuilder();
            builder.Map(matchPath, UseSuccess);
            var app = builder.Build<OwinMiddleware>();
            app.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath + matchPath.Substring(0, matchPath.Length - 1), context.Get<string>("test.PathBase"));
            Assert.Equal(requestPath.Substring(matchPath.Length - 1), context.Get<string>("test.Path"));
        }

        [Theory]
        [InlineData("/foo", "", "")]
        [InlineData("/foo", "/bar", "")]
        [InlineData("/foo", "", "/bar")]
        [InlineData("/foo", "/foo", "")]
        [InlineData("/foo", "/foo", "/bar")]
        [InlineData("/foo", "", "/bar/foo")]
        [InlineData("/foo/bar", "/foo", "/bar")]
        public void PathMismatchFunc_PassedThrough(string matchPath, string basePath, string requestPath)
        {
            IOwinContext context = CreateRequest(basePath, requestPath);
            IAppBuilder builder = new AppBuilder();
            builder.Map(matchPath, UseNotImplemented);
            builder.Use(Success);
            var app = builder.Build<OwinMiddleware>();
            app.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase);
            Assert.Equal(requestPath, context.Request.Path);
        }

        [Theory]
        [InlineData("/foo", "", "")]
        [InlineData("/foo", "/bar", "")]
        [InlineData("/foo", "", "/bar")]
        [InlineData("/foo", "/foo", "")]
        [InlineData("/foo", "/foo", "/bar")]
        [InlineData("/foo", "", "/bar/foo")]
        [InlineData("/foo/bar", "/foo", "/bar")]
        public void PathMismatchAction_PassedThrough(string matchPath, string basePath, string requestPath)
        {
            IOwinContext context = CreateRequest(basePath, requestPath);
            IAppBuilder builder = new AppBuilder();
            builder.Map(matchPath, UseNotImplemented);
            builder.Use(Success);
            var app = builder.Build<OwinMiddleware>();
            app.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase);
            Assert.Equal(requestPath, context.Request.Path);
        }

        [Fact]
        public void ChainedRoutes_Success()
        {
            IAppBuilder builder = new AppBuilder();
            builder.Map("/route1", map =>
            {
                map.Map((string)"/subroute1", UseSuccess);
                map.Use(NotImplemented);
            });
            builder.Map("/route2/subroute2", UseSuccess);
            var app = builder.Build<OwinMiddleware>();

            IOwinContext context = CreateRequest(string.Empty, "/route1");
            Assert.Throws<AggregateException>(() => app.Invoke(context).Wait());

            context = CreateRequest(string.Empty, "/route1/subroute1");
            app.Invoke(context);
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/route1/subroute1", context.Request.Path);

            context = CreateRequest(string.Empty, "/route2");
            app.Invoke(context);
            Assert.Equal(404, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/route2", context.Request.Path);

            context = CreateRequest(string.Empty, "/route2/subroute2");
            app.Invoke(context);
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/route2/subroute2", context.Request.Path);

            context = CreateRequest(string.Empty, "/route2/subroute2/subsub2");
            app.Invoke(context);
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/route2/subroute2/subsub2", context.Request.Path);
        }

        private IOwinContext CreateRequest(string basePath, string requestPath)
        {
            IOwinContext context = new OwinContext();
            context.Request.PathBase = basePath;
            context.Request.Path = requestPath;
            return context;
        }
    }
}