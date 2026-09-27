# 📚 Agile Based Library Circulation Control System

A modern, high-performance web application for managing school library operations, book catalogues, members, borrowing workflows, and analytical reporting. Built with **ASP.NET Core 10 Razor Pages**, **Entity Framework Core 10**, **Microsoft SQL Server**, and styled with **Tailwind CSS v4**.

Features a responsive design with **full Light and Dark Mode support**, instant search and filtering, and an **expandable / collapsible sidebar** optimized for full-screen desktop and mobile experiences.

---

## 📸 Interface Previews

### 1. Public Library Portal & Self-Service Circulation
Students and faculty can discover available literature, check real-time copy counts, and track their personal borrow records without requiring prior administrative login.

| Public Home Screen & Hero Showcase | Browse Books Catalogue & Availability Grid |
| :---: | :---: |
| ![Public Home Screen](docs/screenshots/01.%20public_home_screen.png) | ![Browse Books Catalogue](docs/screenshots/02.%20public_browse_books.png) |

| Member Self-Service Borrowing History | Secure Administrator Login |
| :---: | :---: |
| ![My Borrowing List](docs/screenshots/02.%20my_borrowing_list.png) | ![Admin Login](docs/screenshots/03.%20admin_login.png) |

---

### 2. Analytical Dashboard
Real-time KPI metric tracking, vector borrowing trend curves, live overdue feeds, and interactive membership breakdown.

| Admin Dashboard (Light Mode) | Admin Dashboard (Dark Mode - Electric Cyan) |
| :---: | :---: |
| ![Admin Dashboard Light](docs/screenshots/05.%20admin_dashboard_light.png) | ![Admin Dashboard Dark](docs/screenshots/05.%20admin_dashboard_dark.png) |

---

### 3. Book Catalogue & Inventory Management
Search across titles, authors, categories, and ISBNs with real-time status and genre filters, complete with full cover uploads.

| Book Catalogue & Filtering | Add New Book & Cover Upload |
| :---: | :---: |
| ![Books Catalogue](docs/screenshots/06.%20books_screen.png) | ![Add Book Screen](docs/screenshots/06.%20add_book_screen.png) |

---

### 4. Members Administration
Classified membership tiers for Students, Teachers, and Staff with department and contact tracking.

| Members Directory | Add New Member Registration |
| :---: | :---: |
| ![Members Directory](docs/screenshots/07.%20members_screen_light.png) | ![Add New Member](docs/screenshots/07.%20add_new_member.png) |

---

### 5. Circulation - Borrow & Return Workflows
Transaction-safe checkout and return workflows with real-time stock deduction, inspection condition grading, and automatic overdue fine calculation.

| Borrowing Management Overview | New Book Checkout Flow |
| :---: | :---: |
| ![Borrow Management](docs/screenshots/08.%20borrow_book_screen.png) | ![Make a New Borrow](docs/screenshots/08.%20make_a_new_borrow.png) |

| Book Returns Directory | Process Return & Fine Assessment |
| :---: | :---: |
| ![Returns Directory](docs/screenshots/09.%20return_book_screen.png) | ![Process a Book Return](docs/screenshots/09.%20process_a_book_return.png) |

---

### 6. Circulation History, Reports & Analytics
Comprehensive audit trail of library transactions and customizable periodic reporting.

| Circulation & Borrowing History | Analytics & Periodic Reports |
| :---: | :---: |
| ![Borrowing History](docs/screenshots/10.%20borrowing%20history_screen_dark.png) | ![Reports Screen](docs/screenshots/11.%20report_screen_dark.png) |

---

### 7. Account Profile, Security & System Settings
Administrator profile editing, secure password rotation with complexity validation, and global system configuration.

| Administrator Profile | Password Rotation Flow | Global System Settings |
| :---: | :---: | :---: |
| ![Profile Screen](docs/screenshots/12.%20profile_screen.png) | ![Change Password](docs/screenshots/13.%20change_password_screen.png) | ![Settings](docs/screenshots/14.%20settings.png) |

---

## ✨ Features

### 📊 Real-Time Analytical Dashboard
- **KPI Metrics**: Real-time tracking of Total Books, Active Members, Currently Borrowed Books, Overdue Items, and Fine Revenue.
- **Dynamic Trend Chart**: Interactive monthly and period-based borrowing trends rendered via responsive vector charts with glowing hover data points.
- **Live Feeds**: Fast-access widgets for recent borrowings, overdue returns with calculated day counts, and member distribution breakdowns.

### 📖 Book Catalogue & Visual Covers
- **Catalog Operations**: Full CRUD lifecycle for books with ISBN uniqueness validation, copy inventory, and shelf availability tracking.
- **Smart Search & Filters**: Unified toolbar enabling real-time search across titles, authors, categories, and languages, paired with status and genre dropdown filters.
- **Complete Book Covers**: Custom cover image uploads with full vector SVG artwork for all seeded Myanmar and international titles.

### 👥 Member Administration
- **Multi-Tier Memberships**: Support for **Student**, **Teacher**, and **Staff** member classifications.
- **Department & Contact Tracking**: Manage student IDs, departments, phone numbers, and academic emails.
- **Activity & Status**: One-click member activation/deactivation and detailed member history modals.

### 🔄 Borrow & Return Workflows
- **Circulation Control**: Transaction-safe checkout and return workflows with real-time stock deduction and restoration.
- **Fine Calculation Engine**: Automatic fine calculation for overdue items based on due dates.
- **Condition Grading**: Inspection notes and book condition status tracking (*Good*, *Late*, *Damaged*).

### 🌐 Public Portal & Self-Service Circulation
- **Public Book Discovery & Hero Showcase**: Clean, responsive public showcase (`/Index`) with search toolbar, quick collection metrics, and interactive featured reading showcases.
- **Browse Books Catalogue Grid**: Dedicated catalogue view with dynamic category filtering dropdown, real-time availability badges, publication years, ISBN identifiers, and instant `+ Borrow Book` action cards.
- **Self-Service Borrowing**: Streamlined borrowing checkout flow (`/Public/Borrow`) with instant borrow code confirmation.
- **Personal Borrowing Lookup**: Fast lookup tool (`/Public/MyBorrowings`) allowing members to check active loans, due dates, and return history by Student ID or Email.

### 📈 Reports & Library Settings
- **Periodic Reports**: Filter library activity by current month, previous month, current year, or custom date ranges.
- **Customizable Preferences**: System-wide defaults for reporting periods, theme preferences, and export formats.

### 🌓 Electric Cyan Dark Mode & Collapsible Navigation
- **Electric Cyan Dark Mode**: Contemporary obsidian dark mode (`#0B0F17`) featuring high-contrast **Electric Cyan** accents (`#06B6D4` / `#0891B2`), cyan glow shadows, and vibrant data charts.
- **Ultra-Smooth Theme Switching**: Instant toggling between Light and Dark themes with zero-flash layout hydration (`_ThemeBootstrap.cshtml`) and `localStorage` persistence.
- **Sidebar Ergonomics**: Dual-state sidebar supporting full expanded mode (with labels and active indicators) and compact collapsed mode (icon-only navigation with tooltips).
- **Mobile Off-Canvas**: Responsive drawer with dimmed backdrop support for tablets and mobile devices.

### 🔒 Security & Admin Controls
- **Cookie Authentication**: Strict cookie-based session security with `HttpOnly`, `SameSite=Lax`, and 8-hour sliding expiration.
- **Lockout Protection**: Automatic account lockout after 5 consecutive failed attempts with a 15-minute cooldown.
- **First-Time Password Change**: Mandatory initial password rotation flow with complexity validation (uppercase, lowercase, number, symbol, min 10 chars).
- **Accessible Confirmation Modals**: Keyboard-trapped (Esc, Tab) accessible modals preventing accidental deletion of records.

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Framework** | [ASP.NET Core 10 Razor Pages](https://learn.microsoft.com/aspnet/core/razor-pages/) |
| **Runtime & Language** | .NET 10.0 / C# 13 |
| **Database** | Microsoft SQL Server (LocalDB / Express / Enterprise) |
| **ORM & Migrations** | [Entity Framework Core 10](https://learn.microsoft.com/ef/core/) |
| **Styling & Design** | [Tailwind CSS v4](https://tailwindcss.com/) (Standalone CLI pipeline) |
| **Scripting & DOM** | Vanilla JavaScript (ES6+, zero heavy JS framework dependencies) |
| **Charts** | HTML5 Canvas / Responsive SVG Data Visualizations |
| **Security** | ASP.NET Core Cookie Authentication & Identity PBKDF2 Password Hasher |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v18.0.0 or higher)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) or **SQL Server Express** running locally.

---

### Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/thantzinoo-dev/school-library-management.git
   cd school-library-management
   ```

2. **Configure Connection String:**
   Ensure `appsettings.Development.json` matches your local SQL Server instance:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SchoolLibraryDB;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "SeedAdmin": {
       "Username": "admin",
       "Email": "admin@schoollibrary.edu"
     }
   }
   ```

3. **Install Dependencies & Build Tailwind CSS:**
   ```bash
   npm install
   npm run css:build
   ```

4. **Apply Database Migrations:**
   ```bash
   dotnet ef database update
   ```
   *(The database initializer automatically seeds sample books, members, borrow records, and the default admin account on initial launch.)*

5. **Run the Application:**
   ```bash
   dotnet run
   ```

6. **Open in Browser:**
   Navigate to:
   ```text
   http://localhost:5141
   ```

---

### 🔑 Default Admin Credentials

When the database initializes, the following credentials are ready for use:

| Field | Value |
| :--- | :--- |
| **Username** | `admin` |
| **Email** | `admin@schoollibrary.edu` |
| **Password** | `Admin@123456` |

---

## 📁 Project Structure

```text
School Library Management/
├── Data/
│   ├── ApplicationDbContext.cs       # EF Core DB context with entity configurations & check constraints
│   └── DbInitializer.cs              # Database seeding with Myanmar & international literature
├── Migrations/                       # Entity Framework migration snapshots
├── Models/
│   ├── AdminAccount.cs               # Admin authentication credentials & lockout state
│   ├── AdminProfile.cs               # Admin personal profile & contact info
│   ├── Book.cs                       # Book model with ISBN, copy counts & cover image path
│   ├── BorrowRecord.cs               # Borrow transactions with date tracking & fine calculation
│   ├── LibrarySettings.cs            # Global preferences for themes & report defaults
│   └── Member.cs                     # Library member model with type & department
├── Pages/
│   ├── Books/                        # Book catalogue, create, edit, & details
│   ├── Borrow/                       # Book checkout workflow
│   ├── Members/                      # Member administration & details
│   ├── Profile/                      # Admin profile editor & password rotation
│   ├── Reports/                      # Activity reporting & export indicators
│   ├── Return/                       # Return processing & fine assessment
│   ├── Settings/                     # Application & theme preferences
│   ├── Shared/                       # Layouts, sidebar, theme bootstrap, & confirm dialogs
│   ├── Index.cshtml                  # Dashboard landing view
│   ├── Login.cshtml                  # Secure admin sign-in
│   └── Logout.cshtml                 # Session sign-out handler
├── Services/
│   ├── AdminAuthentication.cs        # Claims principal creation & normalization helpers
│   └── BookCoverStorage.cs           # Cover image validation & physical disk storage
├── Styles/
│   └── app.css                       # Tailwind CSS v4 input design system
├── wwwroot/
│   ├── css/app.css                   # Compiled, minified Tailwind CSS bundle
│   ├── images/books/                 # Fallback SVG book covers
│   ├── js/                           # Theme toggler, sidebar collapse, & confirm modal scripts
│   └── uploads/books/                # Uploaded cover images
├── docs/
│   └── screenshots/                  # High-resolution full-screen interface previews
├── package.json                      # Tailwind CSS v4 CLI build scripts
├── Program.cs                        # Web app bootstrap, middleware, cookie auth, & route security
└── README.md                         # Project documentation
```

---

## 📜 NPM Scripts

| Command | Description |
| :--- | :--- |
| `npm run css:build` | Compiles and minifies `./Styles/app.css` into `./wwwroot/css/app.css`. |
| `npm run css:dev` | Generates development CSS bundle without minification. |
| `npm run css:watch` | Starts Tailwind CLI in continuous watch mode for hot stylesheet rebuilding. |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
