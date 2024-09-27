# Listify

## Description

Listify is a Telegram bot project built with ASP.NET that allows users to manage a personalized list of content such as films, TV shows, cartoons, and anime. The bot enables you to tag, sort, and share your content lists with friends directly through Telegram messages. It offers the flexibility to create custom tags for better organization and provides a seamless way to filter and view content based on various tags (e.g., type, emotions, genres).

## Features

- **Content Management**: Add, update, delete, and view content within your personalized list.
- **Custom Tagging**: Create tags to classify content by type (e.g., film, anime) or emotions (e.g., liked, disliked).
- **Filter and Sort**: Filter your content lists by tags to quickly find what you're looking for.
- **Telegram Sharing**: Easily share your content lists with friends via Telegram.
- **Interactive Menus**: Utilize the Telegram bot interface to manage content and tags in a user-friendly way.

## Technologies Used

- **ASP.NET Core 8.0**: Web framework used to build the bot's server and APIs.
- **Telegram.Bot 21.10.1**: Library for Telegram Bot API interactions.
- **Swashbuckle.AspNetCore 6.7.3**: Swagger generation for API documentation.
- **Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4**: PostgreSQL database provider for Entity Framework Core.
- **Microsoft.EntityFrameworkCore 8.0.8**: ORM for database management.
- **Microsoft.Extensions.Configuration 8.0.0**: Configuration management library.
  
## API Endpoints

### Content Management

- **POST** `/api/Content/createContent/{userId}`: Add new content for a specific user.
- **GET** `/api/Content/getContentsByUserId/{userId}`: Retrieve all content for a specific user.
- **GET** `/api/Content/getContentById/{userId}/{contentId}`: Get details for a specific content item.
- **PUT** `/api/Content/{userId}/{contentId}`: Update existing content.
- **DELETE** `/api/Content/deleteContent/{userId}/{contentId}`: Remove content by its ID.

### Tag Management

- **POST** `/api/Tag/createTag/{userId}`: Create a new tag for a user.
- **GET** `/api/Tag/getTagsByUserId/{userId}`: Get all tags created by a user.
- **PUT** `/api/Tag/updateTagInfoById/{userId}/{tagId}`: Update an existing tag.
- **DELETE** `/api/Tag/deleteTag/{userId}/{tagId}`: Delete a specific tag.

### User Management

- **POST** `/api/Users/CreateUser`: Create a new user.
- **GET** `/api/Users/getUserByID/{userId}`: Retrieve user details.
- **DELETE** `/api/Users/deleteUser/{userId}`: Delete a user by ID.

## How to Install

### Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- PostgreSQL database
- A Telegram bot token from [BotFather](https://core.telegram.org/bots#botfather)
  
### Steps

1. Clone the repository:
    ```bash
    git clone https://github.com/your-repo/listify.git
    cd listify
    ```

2. Install dependencies:
    ```bash
    dotnet restore
    ```

3. Set up PostgreSQL:
    - Ensure you have PostgreSQL installed and running.
    - Create a database and update your connection string in `appsettings.json`.

4. Configure User Secrets:
    ```bash
    dotnet user-secrets set "TelegramBotToken" "<Your-Bot-Token>"
    ```

5. Run migrations to set up the database:
    ```bash
    dotnet ef database update
    ```

6. Run the project:
    ```bash
    dotnet run
    ```

7. Start chatting with your bot on Telegram!

## How to Use

1. **Start a Chat**: Once the bot is deployed, start chatting with your bot on Telegram.
2. **Add Content**: Create content and add it to your list. 
3. **Tag Content**: Create custom tags and associate them with your content to better categorize it.
4. **Filter by Tags**: Use the filtering feature to find specific content based on tags.
5. **Share Content**: Send your lists directly to your friends using Telegram.

## Screenshots

### Main Menu

*Add a screenshot of the bot's main interface here*

### Tag Management

*Add a screenshot showing tag creation and management*

### Content Management

*Add a screenshot showing how content is added and filtered by tags*

## Video Demo

Watch the demo on [YouTube](https://youtu.be/_XW32ZAWyOw).
