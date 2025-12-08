# ClientBooking_QADTSL6 - Local Development Setup (Quick Steps)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Node.js (LTS) + NPM](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

## Clone the Repository

```bash
git clone https://github.com/jMorton95/ClientBooking_QADTSL6.git
cd ClientBooking_QADTSL6
docker compose up
npm install
npm start
Navigate to localhost:5195
Note: After docker compose up, you will need to either open a new Terminal before continuing, or 'control c' to cancel, but ensure the Docker container is running in Docker Desktop 