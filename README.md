# Blog Backend

## Description

This is the backend server for a full-featured blog application. It provides a RESTful API to handle all the core functionalities including post management, user authentication, and comments. It is designed to be consumed by a frontend client like the [Blog Frontend](https://github.com/jahnavisomasundaram/BlogFrontend).

## Features

* **User Authentication:** Secure user registration and login using JSON Web Tokens (JWT).
* **CRUD for Posts:** Full Create, Read, Update, and Delete functionality for blog posts.
* **Protected Routes:** Certain actions like creating or deleting posts are protected and require user authentication.
* **Commenting System:** Allows authenticated users to comment on posts.
* **Data Persistence:** Uses MongoDB to store user, post, and comment data.

## Technologies Used

* **Backend:** Node.js, Express.js
* **Database:** MongoDB with Mongoose ODM, Supabase
* **Authentication:** JSON Web Tokens (JWT)
* **Password Hashing:** bcryptjs
* **Environment Variables:** dotenv
* **CORS:** cors

## Getting Started

Follow these instructions to get a local copy of the server up and running for development and testing.

### Prerequisites

* **Node.js & npm:** You can download them from [nodejs.org](https://nodejs.org/).
* **MongoDB:** You need a running MongoDB instance. You can install it locally or use a cloud service like MongoDB Atlas.

### Installation

1.  **Clone the repository**
    ```sh
    git clone [https://github.com/jahnavisomasundaram/BlogBackend.git](https://github.com/jahnavisomasundaram/BlogBackend.git)
    ```
2.  **Navigate to the project directory**
    ```sh
    cd BlogBackend
    ```
3.  **Install NPM packages**
    ```sh
    npm install
    ```
4.  **Set up environment variables**

    Create a `.env` file in the root directory and add the following configuration variables. Replace the placeholder values with your actual configuration.

    ```
    PORT=5000
    MONGO_URI=your_mongodb_connection_string
    JWT_SECRET=your_super_secret_jwt_key
    ```
    * `MONGO_URI`: Your connection string for the MongoDB database.
    * `JWT_SECRET`: A secret string used to sign the JSON Web Tokens.

5.  **Run the server**
    ```sh
    npm start
    ```

The server will start on `http://localhost:5000` (or the port you specified in your `.env` file).

---

## API Endpoints

The base URL for all API routes is `/api`.

### Authentication Routes (`/api/auth`)

| Method | Endpoint      | Description                  |
| :----- | :------------ | :--------------------------- |
| `POST` | `/register`   | Register a new user.         |
| `POST` | `/login`      | Log in a user and get a JWT. |

### Post Routes (`/api/posts`)

| Method   | Endpoint           | Description                                    | Authentication |
| :------- | :----------------- | :--------------------------------------------- | :------------- |
| `GET`    | `/`                | Get a list of all blog posts.                  | Public         |
| `GET`    | `/:id`             | Get a single blog post by its ID.              | Public         |
| `POST`   | `/`                | Create a new blog post.                        | Required       |
| `PUT`    | `/:id`             | Update an existing blog post.                  | Required       |
| `DELETE` | `/:id`             | Delete a blog post.                            | Required       |

