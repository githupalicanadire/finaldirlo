const http = require("http");
const fs = require("fs");
const path = require("path");
const url = require("url");

const PORT = process.env.PORT || 5000;

// MIME types for different file extensions
const mimeTypes = {
  ".html": "text/html",
  ".css": "text/css",
  ".js": "text/javascript",
  ".json": "application/json",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".gif": "image/gif",
  ".ico": "image/x-icon",
  ".svg": "image/svg+xml",
  ".woff": "font/woff",
  ".woff2": "font/woff2",
};

const server = http.createServer((req, res) => {
  const parsedUrl = url.parse(req.url);
  let pathname = parsedUrl.pathname;

  console.log(`Request: ${req.method} ${pathname}`);

  // Handle root path
  if (pathname === "/") {
    serveInfoPage(res);
    return;
  }

  // Try to serve static files from wwwroot
  if (
    pathname.startsWith("/static/") ||
    pathname.startsWith("/css/") ||
    pathname.startsWith("/js/") ||
    pathname.startsWith("/lib/")
  ) {
    serveStaticFile(pathname, res);
    return;
  }

  // Handle API-like requests by showing information
  if (pathname.startsWith("/api/")) {
    serveApiInfo(pathname, res);
    return;
  }

  // Default response for other paths
  serveInfoPage(res);
});

function serveInfoPage(res) {
  const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>EShop Microservices - Development Info</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
            color: #333;
        }
        .container {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #0066cc;
            border-bottom: 3px solid #0066cc;
            padding-bottom: 10px;
        }
        h2 {
            color: #0088cc;
            margin-top: 30px;
        }
        .status {
            background: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 5px;
            padding: 15px;
            margin: 20px 0;
        }
        .error {
            background: #f8d7da;
            border: 1px solid #f5c6cb;
            color: #721c24;
        }
        .info {
            background: #d1ecf1;
            border: 1px solid #bee5eb;
            color: #0c5460;
        }
        .success {
            background: #d4edda;
            border: 1px solid #c3e6cb;
            color: #155724;
        }
        code {
            background: #f8f9fa;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }
        pre {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
            border-left: 4px solid #0066cc;
        }
        .service-list {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
            margin: 20px 0;
        }
        .service-card {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid #28a745;
        }
        .port {
            font-weight: bold;
            color: #007bff;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🏪 EShop Microservices Development Server</h1>
        
        <div class="status error">
            <strong>⚠️ Development Environment Issue</strong><br>
            This is a .NET Core microservices project, but the current environment doesn't have .NET Core or Docker installed.
        </div>

        <h2>📋 Project Information</h2>
        <p>This is an <strong>E-commerce Microservices</strong> application built with:</p>
        <ul>
            <li>🎯 <strong>.NET 8.0</strong> - Primary framework</li>
            <li>🏗️ <strong>Microservices Architecture</strong> - DDD, CQRS, Clean Architecture</li>
            <li>🐳 <strong>Docker</strong> - Containerization</li>
            <li>🔄 <strong>Event-Driven Communication</strong> - RabbitMQ</li>
            <li>🚪 <strong>API Gateway</strong> - YARP Reverse Proxy</li>
        </ul>

        <h2>🏗️ Microservices Architecture</h2>
        <div class="service-list">
            <div class="service-card">
                <h3>🛍️ Shopping Web UI</h3>
                <p>ASP.NET Core Web Application</p>
                <p class="port">Port: 6005</p>
            </div>
            <div class="service-card">
                <h3>🚪 API Gateway</h3>
                <p>YARP Reverse Proxy</p>
                <p class="port">Port: 6004</p>
            </div>
            <div class="service-card">
                <h3>📦 Catalog Service</h3>
                <p>Product catalog management</p>
                <p class="port">Port: 6000</p>
            </div>
            <div class="service-card">
                <h3>🛒 Basket Service</h3>
                <p>Shopping cart functionality</p>
                <p class="port">Port: 6001</p>
            </div>
            <div class="service-card">
                <h3>💰 Discount Service</h3>
                <p>gRPC discount service</p>
                <p class="port">Port: 6002</p>
            </div>
            <div class="service-card">
                <h3>📋 Ordering Service</h3>
                <p>Order management</p>
                <p class="port">Port: 6003</p>
            </div>
        </div>

        <h2>🛠️ Required Tools for Development</h2>
        <div class="status info">
            To properly run this project, you need:
            <ol>
                <li><strong>.NET 8.0 SDK</strong> - Download from <a href="https://dotnet.microsoft.com/download" target="_blank">Microsoft</a></li>
                <li><strong>Docker Desktop</strong> - Download from <a href="https://www.docker.com/products/docker-desktop" target="_blank">Docker</a></li>
                <li><strong>Visual Studio 2022</strong> (recommended) or <strong>VS Code</strong></li>
            </ol>
        </div>

        <h2>🚀 How to Run the Project</h2>
        <div class="status success">
            <strong>Option 1: Using Docker (Recommended)</strong>
            <pre>cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d</pre>
            Then access: <a href="http://localhost:6005" target="_blank">http://localhost:6005</a>
        </div>

        <div class="status info">
            <strong>Option 2: Using .NET CLI</strong>
            <pre># Restore packages
cd src
dotnet restore

# Run individual services
cd WebApps/Shopping.Web
dotnet run</pre>
        </div>

        <h2>🔗 Important URLs (when running with Docker)</h2>
        <ul>
            <li>🛍️ <strong>Shopping Web UI:</strong> <a href="http://localhost:6005" target="_blank">http://localhost:6005</a></li>
            <li>🚪 <strong>API Gateway:</strong> <a href="http://localhost:6004" target="_blank">http://localhost:6004</a></li>
            <li>🐰 <strong>RabbitMQ Dashboard:</strong> <a href="http://localhost:15672" target="_blank">http://localhost:15672</a> (guest/guest)</li>
            <li>🗄️ <strong>PostgreSQL:</strong> localhost:5432, localhost:5433</li>
            <li>🗄️ <strong>SQL Server:</strong> localhost:1433</li>
            <li>🔄 <strong>Redis:</strong> localhost:6379</li>
        </ul>

        <h2>📚 Documentation & Learning</h2>
        <p>For detailed information about this microservices architecture:</p>
        <ul>
            <li>📖 <a href="https://medium.com/@mehmetozkaya/net-8-microservices-ddd-cqrs-vertical-clean-architecture-2dd7ebaaf4bd" target="_blank">Medium Article</a></li>
            <li>🎓 <a href="https://www.udemy.com/course/microservices-architecture-and-implementation-on-dotnet/" target="_blank">Udemy Course</a></li>
            <li>💻 <a href="https://github.com/aspnetrun/run-aspnetcore-microservices" target="_blank">GitHub Repository</a></li>
        </ul>

        <div class="status">
            <strong>💡 Current Status:</strong> Development server running on port ${PORT}<br>
            This is a placeholder server providing project information since .NET runtime is not available.
        </div>
    </div>
</body>
</html>`;

  res.writeHead(200, { "Content-Type": "text/html" });
  res.end(html);
}

function serveApiInfo(pathname, res) {
  const apiInfo = {
    message: "This is a .NET Core microservices project",
    requestedPath: pathname,
    availableServices: {
      "Shopping.Web": "http://localhost:6005",
      "API Gateway": "http://localhost:6004",
      "Catalog.API": "http://localhost:6000",
      "Basket.API": "http://localhost:6001",
      "Discount.Grpc": "http://localhost:6002",
      "Ordering.API": "http://localhost:6003",
    },
    setup: "Please use Docker to run the full microservices stack",
    command:
      "docker-compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d",
  };

  res.writeHead(200, { "Content-Type": "application/json" });
  res.end(JSON.stringify(apiInfo, null, 2));
}

function serveStaticFile(pathname, res) {
  // Try to find static files in the wwwroot directory
  const staticPath = path.join(
    __dirname,
    "src/WebApps/Shopping.Web/wwwroot",
    pathname.replace(/^\/static/, ""),
  );

  fs.readFile(staticPath, (err, data) => {
    if (err) {
      res.writeHead(404, { "Content-Type": "text/plain" });
      res.end("File not found");
      return;
    }

    const ext = path.extname(staticPath);
    const contentType = mimeTypes[ext] || "application/octet-stream";

    res.writeHead(200, { "Content-Type": contentType });
    res.end(data);
  });
}

server.listen(PORT, () => {
  console.log(`🚀 EShop Microservices Development Server`);
  console.log(`📍 Server running at http://localhost:${PORT}`);
  console.log(
    `⚠️  Note: This is a .NET Core project. For full functionality, use Docker.`,
  );
  console.log(
    `🐳 Run: docker-compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d`,
  );
});
