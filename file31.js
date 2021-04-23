/*
 * Copyright (c) 2020 Alan Badillo Salas <dragonnomada@gmail.com>
 * MIT Licensed
 */

const version = "v1.0.20";

const fs = require("fs");
const path = require("path");
const http = require("http");
const https = require("https");

const dotenv = require("dotenv");
const express = require("express");
const bodyParser = require("body-parser");
const cors = require("cors");
const busboy = require("connect-busboy");

const fileUploadHandler = require("./lib/fileUploadHandler");

dotenv.config(path.join(process.cwd(), ".env"));

const handleResult = (resolve, reject) => (error, result) => {
    if (error) {
        reject(error);
        return;
    }
    resolve(result);
};

const readFileAsync = (route, encoding = "utf-8", options = {}) => new Promise((resolve, reject) => {
    fs.readFile(route, { encoding, ...options }, handleResult(resolve, reject));
});

let secret = "NYFQ7giCbuwqdGfg78d3vrZ8zHiLj06QglZlTAsLYT";

const createInstance = (server, app = null) => {
    return {
        lib: {},
        containers: {},
        require: server ? server.require : null,
        server,
        app: app || (server ? server.app : null),
        protocol: server ? server.protocol : "virtual",
        port: null,
        host: null,
        domain: null,
        addLibrary(lib) {
            this.lib = {
                ...this.lib,
                ...lib
            };
        },
        async loadContainers() {
            await new Promise(resolve => {
                fs.mkdir(".ballena", { recursive: true }, (error, result) => resolve({ error, result }))
            });

            const containers = await new Promise((resolve, reject) => {
                fs.readFile(
                    path.join(process.cwd(), ".ballena", "containers.json"),
                    (error, result) => {
                        if (error) {
                            reject(error);
                            return;
                        }
                        resolve(JSON.parse(result));
                    }
                );
            }).catch(() => ({}));

            for (let container of Object.keys(containers)) {
                this.addContainer(container);
            }
        },
        async start(port = 4000, host = "0.0.0.0", domain = "localhost") {
            console.log("@ballena/server", version);

            console.log("Secret:", secret);

            this.port = port;
            this.host = host;
            this.domain = domain;

            await this.loadContainers();

            return await new Promise(resolve => {
                this.server.listen(this.port, this.host, () => {
                    this.serverStartedAt = new Date();
                    console.log(`Server started at ${this.protocol}://${this.host}:${this.port}/`);
                    console.log(`                  ${this.protocol}://${this.domain}:${this.port}/`);
                    resolve(this);
                });
            });
        },
        async stop() {
            return await new Promise(resolve => {
                this.server.close(() => {
                    this.serverStoppedAt = new Date();
                    console.log(`Server stopped at ${this.protocol}://${this.host}:${this.port}/`);
                    console.log(`                  ${this.protocol}://${this.domain}:${this.port}/`);
                    resolve(this);
                });
            });
        },
        async restart(handle = (() => null)) {
            await this.stop();
            await handle(this);
            await this.start(this.port, this.host, this.domain);
        },
        addPanel(masterCode) {
            secret = masterCode || Math.random().toString(32).slice(2);

            const container = this.createContainer("@ballena", {
                local: true
            });

            if (this.app) {
                this.app.use(container.router);
            }

            return container;
        },
        removeContainer(name) {
            if (!this.containers[name]) throw new Error(`ballena/error: invalid container "${name}"`);
            // this.containers[name].closed = true;
        },
        createContainer(name, options = {}) {
            options.basePath = options.local ? __dirname : options.basePath || process.cwd();

            const router = express.Router();

            this.containers[name] = {
                ...(this.containers[name] || {}),
                name,
                options,
                router
            };

            router.use(`/${name}`, (request, response, next) => {
                if (!this.containers[name]) {
                    response.status(404).send(`<pre>Cannot ${request.method} /${name}${request.path} (closed)</pre>`);
                    return;
                }
                next();
            });
            router.use(`/${name}`, express.static(path.join(options.basePath, name, "view")));
            router.use(`/${name}/view/`, async (request, respose, next) => {
                const viewName = request.path.replace(/\/$/g, "/index.html");
                const ext = (viewName.match(/\.\w+$/) || [])[0] || ".html";
                const filename = viewName.replace(/^\/|\.\w+$/g, "");

                const code = await readFileAsync(path.join(options.basePath, name, "view", `/${filename}${ext}`)).catch(() => {
                    return null;
                });

                if (!code) {
                    respose.send({
                        ballena: version,
                        container: name,
                        view: filename,
                        ext,
                        exists: true,
                        error: `ballena/error: view ${name}/${filename}${ext} is not exists`,
                        result: null
                    });
                    return;
                }

                respose.send({
                    ballena: version,
                    container: name,
                    view: filename,
                    exists: true,
                    error: null,
                    result: code.replace(/\r/g, "")
                });
            });
            router.use(`/${name}/api/`, async (request, respose, next) => {
                const files = await fileUploadHandler(request);

                const apiName = request.path.replace(/\/$/g, "/index");

                const filename = apiName.replace(/^\//, "");

                const protocol = {
                    ballena: version,
                    container: name,
                    api: filename,
                    exists: true,
                    error: null,
                    result: null,
                    files,
                    async handler() { },
                    output(handler) {
                        protocol.handler = handler;
                    },
                    logs: []
                };

                const code = await readFileAsync(path.join(options.basePath, name, "api", `/${filename}.js`)).catch(() => {
                    protocol.exists = false;
                    return `throw new Error("ballena/error: api ${name}/${filename} is not exists")`;
                });

                const input = key => {
                    const self = {
                        ...(request.query || {}),
                        ...(request.body || {}),
                    };
                    if (!key || key === "self") return self;
                    if (key === "files") return files;
                    return self[key];
                };

                // console.log("code", code);

                try {
                    protocol.result = await new Function(
                        "server",
                        "containers",
                        "container",
                        "protocol",
                        "input",
                        "files",
                        "require",
                        "request",
                        "response",
                        "next",
                        "authorize",
                        "lib",
                        ...Object.keys(this.lib),
                        `return (async () => {
                            ${code}
                        })();`
                    )(
                        this,
                        options.local ? this.containers : {
                            [name]: this.containers[name]
                        },
                        this.containers[name],
                        protocol,
                        input,
                        files,
                        (name, token, ...params) => {
                            if (options.local) {
                                if (name === "ballena") return protocol;
                                return require(name, ...params);
                            }
                            if (protocol.secret === secret) {
                                return require(name, token, ...params);
                            }
                            if (token !== secret) throw new Error(`Unauthorized [@ballena/server/require]`);
                            if (name === "ballena") return protocol;
                            return this.require(name, ...params);
                        },
                        request,
                        respose,
                        next,
                        async handler => {
                            const userToken = input("token") || "";

                            const [name, token] = userToken.split(".");

                            handler = handler || (() => {
                                if (token !== secret) throw new Error(`Unauthorized [@ballena/server/api]: Invalid secret`);
                            });

                            await new Promise(resolve => {
                                fs.mkdir(".ballena", { recursive: true }, (error, result) => resolve({ error, result }))
                            });
                            
                            const users = await new Promise((resolve, reject) => {
                                fs.readFile(
                                    path.join(process.cwd(), ".ballena", "users.json"),
                                    (error, result) => {
                                        if (error) {
                                            reject(error);
                                            return;
                                        }
                                        resolve(JSON.parse(result).users);
                                    }
                                );
                            }).catch(() => []);

                            users.push({
                                name: "master",
                                token: secret,
                                permissions: "*",
                                keys: "*",
                            });

                            const user = users.find(user => {
                                return user.name === name && user.token === token;
                            });

                            if (!user) throw new Error(`Unauthorized [@ballena/server/api]: Invalid token`);

                            user.hasPermission = protocol => {
                                if (user.permissions === "*") return true;
                                return user.permissions.some(permissionProtocol => {
                                    for (let [key, value] of Object.entries(protocol || {})) {
                                        if (permissionProtocol[key] !== value) return false;
                                    }
                                    return true;
                                })
                            };
                            
                            user.hasKey = protocol => {
                                if (user.keys === "*") return true;
                                return user.keys.some(keyProtocol => {
                                    for (let [key, value] of Object.entries(protocol || {})) {
                                        if (keyProtocol[key] !== value) return false;
                                    }
                                    return true;
                                })
                            };

                            let authorized = null;

                            try {
                                authorized = await handler(user);
                            } catch (error) {
                                throw new Error(`Unauthorized [@ballena/server/api]: ${error}`);
                            }
                            if (!authorized) throw new Error(`Unauthorized [@ballena/server/api]`);

                            return user;
                        },
                        this.lib,
                        ...Object.values(this.lib),
                    ).catch(error => {
                        protocol.error = `${error}`.replace(/^Error:\s*/, "");
                        return null;
                    });
                } catch (error) {
                    protocol.error = `${error}`.replace(/^Error:\s*/, "");
                    protocol.code = code;
                }

                const handler = typeof protocol.handler === "function" ? protocol.handler : () => protocol.handler;
                try {
                    protocol.output = await handler(protocol);
                } catch (error) {
                    protocol.error = `${error}`.replace(/^Error:\s*/, "");
                }

                delete protocol.handler;

                if (protocol.aborted) return;

                if (protocol.output) {
                    protocol.result = protocol.output;
                    delete protocol.output;
                }

                try {
                    respose.send(protocol);
                } catch (error) {
                    respose.status(500).send(`${error}`);
                }
            });

            return this.containers[name];
        },
        addContainer(name) {
            const container = this.createContainer(name);

            if (this.app) {
                this.app.use(container.router);
            }

            return container;
        }
    };
};

module.exports = {
    require,
    createInstance,
    config(require) {
        this.require = require;
    },
    createApp() {
        dotenv.config(path.join(process.cwd(), ".env"));

        const app = express();

        // app.get("/", (request, response) => {
        //     response.send(`Hello ${request.query.name || "you"}`);
        // });

        app.use("/", express.static(path.join(process.cwd(), "public")));
        app.use("/static", express.static(path.join(process.cwd(), "static")));
        app.use("/files", express.static(path.join(process.cwd(), "files")));
        app.use("/temp", express.static(path.join(process.cwd(), "temp")));
        app.use("/cdn", express.static(path.join(process.cwd(), "cdn")));
        app.use(cors());
        app.use(bodyParser.json());
        app.use(bodyParser.urlencoded({ limit: "50mb", extended: false }));
        app.use(busboy());

        this.app = app;

        return this.app;
    },
    httpServer() {
        const app = this.createApp();

        const server = http.createServer(app);

        server.app = app;
        server.protocol = "http";
        server.require = this.require;

        return this.createInstance(server);
    },
    httpsServer(options) {
        const app = this.createApp();

        const server = https.createServer(options, app);

        server.app = app;
        server.protocol = "https";
        server.require = this.require;

        return this.createInstance(server);
    },
    quickServer(config = {}) {
        let server = null;

        if (config.options) {
            server = this.httpsServer(options);
        } else {
            server = this.httpServer();
        }

        const masterCode = config.masterCode || "9bc28c3014103c7d60e465d4ab29674184b58c4f149024974fd25a540581b60bf738ee5dfdaf89f09b5b48f74717c681eb0b";

        delete process.env.MASTER_TOKEN;

        server.addPanel(masterCode);

        const port = config.port || process.env.PORT;
        const host = config.host || process.env.HOST;
        const domain = config.domain || process.env.DOMAIN;

        server.start(port, host, domain);

        return server;
    },
    output() {
        console.warn(`ballena/output: single mode is not supported`);
    }
};