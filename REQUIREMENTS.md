**Product Requirements Document (PRD)**
**Product Name**: *TeamStride*
**Prepared by**: Scott Moody
**Date**: May 25, 2025
**Version**: 1.0

---

### **1\. Purpose**

TeamStride is a mobile-first, multi-team SaaS application that empowers coaches to efficiently manage running teams. The application provides features for managing rosters, schedules, training plans, communications, gear, payments, and integrations with Garmin and MileSplit.

---

### **2\. Stakeholders**
* Product Owner
* Development Team
* UX/UI Designer
* QA Engineer
* Sales & Marketing
* End Users: Coaches, Athletes, Parents

  ---

  ### **3\. Objectives**
* Simplify team management for coaches
* Deliver a seamless mobile-first experience
* Integrate with Garmin and MileSplit for real-time performance data
* Provide subdomain-branded portals for teams
* Enable role-based access control and team switching
* Ensure secure, scalable, maintainable architecture

  ---

  ### **4\. Key Features**

  #### **4.1 User Management**
* Registration/Login/Logout
* Microsoft Identity Framework for authentication
* OAuth2 delegated authentication for Microsoft, Google, Facebook, and Twitter
* Role-based access control (Coach, Athlete, Parent, Admin, Host)
* Multi-team access and team switching
* Default team preference
* Auto-team selection via subdomain login
* Onboarding wizard for new users and teams

  #### **4.2 Multi-Tenancy**
* Subdomain support per team (e.g., `teamX.teamstride.com`)
* Custom-branded login, home, and registration pages
* Team switcher UI
* Team-aware permissions and session management

  #### **4.3 Rosters**
* Add/edit/delete athlete profiles
* Import/export rosters (Premium only)
* Assign roles (Athlete, Captain, Parent)

  #### **4.4 Practice Schedules**
* Add/edit/delete practices
* Support for recurring events
* Notification system for schedule updates
* Conflict detection and resolution for overlapping schedules

  #### **4.5 Race Schedules**
* Race listings with time, location, team assignments
* Calendar and list views
* Pre-race checklist and notes
* Real-time race day tools and check-in feature

  #### **4.6 Training Plans**
* Team-wide or individual training plans (available in Free and Standard tiers)
* Group-based training plans (Premium tier only)
* Week-by-week planning
* Integration roadmap for Garmin sync
* Athlete goal tracking and manual workout entry

  #### **4.7 Race Results**
* Manual entry or MileSplit integration (Premium only)
* Athlete stats, personal records, team averages
* Leaderboards

  #### **4.8 Uniforms & Spirit Wear**
* Track sizes, inventory, distribution
* External store linking (optional)

  #### **4.9 Dues & Fees**
* Track payments for uniforms, trips, memberships
* PayPal integration
* Payment history & receipts

  #### **4.10 Messaging**
* Send messages to teams, groups, or individuals
* Support for **email-based messaging** (via SendGrid) and **SMS/text-based messaging** (via Twilio)
* Notification support for schedule changes and announcements
* Read receipts and message history
* Notification preference management per user
* In the Free tier, messaging operates in **demo mode**: no messages are dispatched, only simulated

  #### **4.11 Integrations**
* **Garmin**: Pull workout data (mileage, pace, HR) (Premium only)
* **MileSplit**: Race results auto-import (Premium only)
* **PayPal**: Payment processing
* **OAuth2 Providers**: Delegated user authentication via Microsoft, Google, Facebook, and Twitter
* **Twilio**: SMS messaging support
* **SendGrid**: Email messaging support

  #### **4.12 Reports**
* **Scheduling Conflicts**: Identify overlapping events or practice conflicts
* **Activity Tracking**: Summarize athlete participation and training history
* **Unpaid Fees**: Report outstanding dues by athlete or parent
* **Uniform Orders**: Track uniform sizes, orders, and distribution status
* **Spirit Wear Orders**: Monitor orders and fulfillment progress
* **Parent Logins**: Track engagement and access frequency
* **Athlete Logins**: Report on athlete activity in the system
* **Coach Logins**: Audit trail for coach participation and system usage

  ---

  ### **5\. Architecture Overview**

  #### **5.1 Backend (.NET Stack)**

* **Framework**: .NET 8.x
* **ORM**: Entity Framework Core (Code First)
* **Auth**: Microsoft Identity Framework with OAuth2 support for Microsoft, Google, Facebook, and Twitter
* **Swagger**: API documentation
* **Mapping**: AutoMapper
* **Testing**: XUnit, Moq
* **Database**: SQL Server or PostgreSQL
* **Messaging Integrations**: Twilio for SMS, SendGrid for email

  ##### **Backend Layer Structure**

1. **Domain Layer**: Entities, Enums, Domain Services, Interfaces
2. **Application Layer**: DTOs, Interfaces, Application Services
3. **Infrastructure Layer**: Repositories,
4. **API Layer**: Controllers, Routing

### Dependecy Rules
- Domain has no project dependences
- Application depends only on Domain
- Infrastructure depends on both Domain and Application
- Api depends only on Infrastructure

Data Access (Infrastructure) references Domain
Application references Data Access (Infrastructure)
API references Application

   #### **5.2 Frontend (Next.js)**
* **Framework**: Next.js
* **Styling**: Tailwind CSS
* **Mobile First** design
* **Testing**: Jest, Playwright (optional)

  ---

  ### **6\. Marketing & Sales Site**
* Public landing page with call-to-action
* Feature overview & testimonials
* Pricing plans
* Team signup workflow that routes to subdomain
* Outline of the differences in subscription tiers with clear benefits for Premium users (e.g., automated data sync, unlimited athletes, group training plans, reporting access)

  ---

  ### **7\. Roles & Permissions**
| Role | Description | Permissions |
| ----- | ----- | ----- |
| Host | Application owner | Full platform-wide access and team-level administration |
| Admin | Team administrator | Full access to team features including assigning roles |
| Coach | Team coach | Manage rosters, practices, races; no access to payments |
| Athlete | Team member | View-only access to training, races, results |
| Parent | Guardian of athlete | View rosters, schedules, payments |

  ---

  ### **8\. UI/UX Considerations**
* Mobile-first responsive layout
* Team branding (colors, logo) per subdomain
* Reusable components via Tailwind
* Team switcher UI in nav/header
* List and calendar views for schedule data
* OAuth2 login buttons for delegated authentication
* SMS and email communication preferences
* Parent/athlete-friendly interface options

  ---

  ### **9\. Test Strategy**

  #### **9.1 Testing Layers**
* **Domain**: Business logic unit tests
* **Application**: DTO orchestration tests
* **Infrastructure**: Repository tests

* **Frontend**: UI integration and interaction tests

  #### **9.2 Test Design Patterns**
* Inheritance-based organization
* Base test classes for shared logic
* AAA pattern: Arrange → Act → Assert

  ---

  ### **10\. Milestones**

| Phase | Duration | Deliverables |
| ----- | ----- | ----- |
| Planning & Discovery | 2 weeks | Final PRD, Architecture Map |
| Backend/API MVP | 4 weeks | Auth, Rosters, Schedules |
| Frontend MVP | 4 weeks | Mobile-first UI, core features |
| Integrations | 3 weeks | Garmin, MileSplit, PayPal, OAuth2, Messaging |
| Branding & Subdomains | 2 weeks | Custom themes, branded pages |
| QA \+ User Testing | 2 weeks | Bug fixes, feedback, regression tests |
| Launch | 1 week | Production deployment, monitoring setup |
| Deployment Setup | 1 week | GitHub Actions CI/CD, Azure Cloud hosting, DEV/QA/PRD environments |
| Accessibility Review | 1 week | Ensure WCAG 2.1 A compliance where feasible |
| Localization Planning | 1 week | Prepare for multilingual UI in future phase |
| Delivery Platform | 1 week | Mobile Web only (no native app or PWA plans) |

**Note on Messaging in Free Tier**  
 In the Free tier, all messaging functions (SMS and email) will operate in **demo mode**. Messages will be logged and viewable within the application UI but **will not be dispatched to recipients**. This allows coaches to preview the messaging functionality without incurring actual communication costs. Implementation should leverage a feature flag or subscription check to route messages through a mock sender instead of the live Twilio or SendGrid services.

---

### **11\. Non-Functional Requirements**
* **Scalability**: Horizontal scalability with multi-tenancy
* **Security**: Encrypted storage, secure login/auth, delegated auth support
* **Performance**: Mobile load time \< 2s
* **Availability**: 99.9% uptime
* **Maintainability**: Clean codebase with test coverage

  ---

  ### **12\. Future Enhancements**
* Push notifications (Firebase)
* Offline mode via PWA support
* Calendar sync (Google, Apple)
* AI-based training analytics
* Stripe integration (broader payment options)
* **Multilingual UI support**
* **Public team pages for sharing results and announcements**
* **In-app analytics dashboard for coaches**
* **Custom forms for meet registrations and waivers**
* **Athlete attendance tracking**
* **Video upload and replay support for race reviews**
* **Open API for custom team integrations**

  ---

  ### **13\. Subscription Tiers**

| Tier | Athletes Limit | Features |
| ----- | ----- | ----- |
| Free | 7 | Basic team setup, manual data entry only; messaging is demo-only |
| Standard | 30 | Manual data entry for schedules, rosters, results |
| Premium | Unlimited | Import/export data, Garmin & MileSplit sync, full messaging & reporting |

  ---

  ### **14\. Cross-Cutting Concerns**
* **Auditing**: Track creation/modification metadata on key entities (who, what, when)
* **Logging**: Centralized, structured logging using a provider like Serilog or ELK stack; logs should include request correlation IDs
* **Security**:
  * OAuth2 delegated authentication for major identity providers
  * Encrypted tokens and user data
  * Role-based access control and team isolation
  * HTTPS/TLS enforced for all endpoints

* **Exception Handling**:
  * Global exception middleware to handle and log uncaught exceptions
  * Consistent error response model (error codes, messages)
  * Alerting on critical exceptions via external monitoring services (e.g., Application Insights, Sentry)

  ### **15\. Legal & Compliance**
* **Terms of Use**: All users must agree to the TeamStride Terms of Use during registration. These terms define acceptable use, liability limitations, data rights, and dispute resolution.
* **Privacy Policy**: A comprehensive privacy policy will be provided, detailing what data is collected, how it is used, stored, and shared. Special attention will be given to third-party data flows (e.g., Garmin, MileSplit, Twilio, SendGrid, PayPal).
* **Parental Consent**: Given that athletes may be minors, the platform will include COPPA (Children’s Online Privacy Protection Act) compliance features:
  * Parents must approve account creation for users under 13\.
  * Coaches will be required to indicate whether athletes are minors during roster creation.
  * TeamStride will provide digital parental consent forms and logs.

* **Data Protection**:
  * All data will be stored using industry-standard encryption at rest and in transit.
  * Personally Identifiable Information (PII) is protected and access-controlled.
  * Data retention policies will allow coaches/admins to delete data permanently per GDPR/CCPA requirements.

* **Third-Party Compliance**:
  * Integrations with PayPal, Twilio, SendGrid, Garmin, and MileSplit will adhere to their respective data and API usage terms.
  * OAuth2 identity providers will be used in accordance with their developer agreements and privacy guidelines.

  ---

  ### **16\. Billing & Subscription Management**
* **Subscription Plans**: TeamStride offers three SaaS subscription tiers: Free, Standard, and Premium.
  * Free: Limited to 7 athletes, messaging is demo-only.
  * Standard: Up to 30 athletes, manual data entry only.
  * Premium: Unlimited athletes, automated integrations, import/export capabilities.

* **Subscription Management Features**:
  * Admin users can view current plan, renewal date, and upgrade/downgrade options.
  * In-app upgrade workflow with checkout via PayPal.
  * Billing history and downloadable receipts.
  * Usage limits and feature access enforced via backend checks and feature flags.

* **Trial Periods & Expiration**:
  * Optional 14-day free trial of Premium features.
  * Grace period and downgrade to Free upon expiration if payment fails.

* **Invoicing & Payment Integration**:
  * PayPal subscription APIs for recurring billing.
  * Invoices automatically emailed to Admins.
  * Prorated upgrades or downgrades handled via PayPal adjustments.

* **Compliance**:
  * Billing data stored in accordance with PCI-DSS and GDPR standards.
  * Users must accept billing terms prior to subscription activation.

* **Planned Payment Option**: Stripe will be added in the future as an alternative to PayPal for subscription billing and one-time payments.

