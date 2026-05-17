# SafeZone Tactical Command Center — UI/UX Redesign Spec

**Date:** 2026-05-16  
**Author:** OpenCode Agent  
**Status:** Draft — Awaiting Review  
**Project:** SafeZone (CS-284L Visual Programming)  
**Technology:** C# 12, .NET 8, Blazor Server, Tailwind CSS, Three.js

---

## 1. Design Vision

Transform SafeZone from a functional safety reporting app into an immersive **Tactical Command Center** that feels like a high-end mission control system. Every screen should communicate: *"You are in command. The system is alive."*

**Core Feel:** Dark, dense, glowing, alive. Think NASA/JPL mission control meets cyberpunk HUD — but polished, readable, and professional.

**Success Criteria:**
- First-time visitors say "wow" within 3 seconds
- Daily users feel empowered and in control
- All functional requirements remain intact
- Performance stays smooth (60fps animations)
- Works fully on Blazor Server + C# — no React/Vue

---

## 2. Visual Design System

### 2.1 Color Palette — "Tactical Neon"

| Token | Hex | Role |
|-------|-----|------|
| `--void-black` | `#050508` | Deepest background, page base |
| `--deep-navy` | `#0A0A14` | Main content background (current) |
| `--surface` | `#12121F` | Card backgrounds |
| `--elevated` | `#1A1A2E` | Hover states, elevated surfaces |
| `--neon-green` | `#00FF88` | Primary actions, safe status, success |
| `--electric-cyan` | `#00D4FF` | Info, highlights, links, focus states |
| `--alert-red` | `#FF3366` | Critical, danger, SOS, errors |
| `--warning-amber` | `#FFB800` | Medium severity, warnings |
| `--command-purple` | `#8B5CF6` | Authority features, admin, superadmin |
| `--glass-border` | `rgba(255,255,255,0.10)` | Subtle borders |
| `--glass-border-strong` | `rgba(255,255,255,0.20)` | Active borders |

**Glow System:** Every accent color has a corresponding dim/glow variant:
- `--neon-green-dim`: `rgba(0,255,136,0.15)` for backgrounds
- `--neon-green-glow`: `rgba(0,255,136,0.30)` for box-shadows
- Same pattern for all accent colors

### 2.2 Typography

| Role | Font | Usage |
|------|------|-------|
| **Display** | `Space Grotesk` (existing) | Page titles, headers, hero text |
| **Body** | `Inter` (existing) | Labels, descriptions, body text |
| **Data/Mono** | `JetBrains Mono` (existing) | Incident numbers, timestamps, coordinates, status values |
| **Terminal** | `JetBrains Mono` + uppercase | Status labels, system messages, module headers |

**Typography Rules:**
- Terminal-style labels: `uppercase`, `letter-spacing: 0.1em`, `font-size: 0.7rem`, `color: var(--text-3)`
- Data values: `font-family: var(--font-mono)`, `font-weight: 600`, slightly larger than labels
- Headings: `font-family: var(--font-display)`, `font-weight: 700-800`

### 2.3 Effects & Textures

| Effect | Implementation | Usage |
|--------|----------------|-------|
| **Animated Grid** | CSS `radial-gradient` background with `animation: grid-pan` | All dashboard page backgrounds |
| **Scan Lines** | CSS repeating-linear-gradient overlay, opacity 0.03 | Global subtle texture on all pages |
| **Glowing Borders** | `box-shadow: 0 0 20px <color-glow>` on hover/active | Cards, buttons, active nav items |
| **Glass Morphism 2.0** | Current glass + `box-shadow: 0 8px 32px rgba(<color>, 0.15)` behind | All cards and panels |
| **Ambient Particles** | Lightweight Three.js `Points` geometry, 200 particles, slow orbit | Dashboard backgrounds only |
| **Pulse Glow** | CSS `animation: pulse-glow 2s ease-in-out infinite` | Active status dots, SOS buttons |
| **Shimmer** | CSS `linear-gradient` sweep with `animation: shimmer 1.5s infinite` | Skeleton loaders |

### 2.4 Spacing

- **Base unit:** 8px
- **Card padding:** 20px (tighter than current 24px for denser command feel)
- **Card gap:** 16px
- **Section gap:** 24px
- **Page padding:** 24px 32px

---

## 3. Component Architecture

### 3.1 Command Sidebar (`AppSidebar.razor` + `DashboardLayout.razor`)

**Structure:**
- Width: 260px (desktop), 64px (collapsed), full-width slide-in (mobile)
- Background: `--deep-navy` with subtle right-edge glow
- Border-right: 1px solid `--glass-border` with colored glow on active section

**Elements:**
1. **Logo Mark** (top): Animated SZ mark with slow rotation, system status text below
2. **Module Groups**: Collapsible sections with uppercase labels ("MAIN", "OPERATIONS", "SYSTEM")
3. **Nav Items**: Icon + label + tiny status dot
   - **Idle:** Subtle opacity (0.7), no glow
   - **Hover:** Icon scales 1.1, label brightens, left border glows (4px wide)
   - **Active:** Persistent accent glow, label fully bright, slight rightward shift (4px)
4. **System Status Strip** (bottom): Online indicator dot + "SYSTEM ONLINE" + version

**Animations:**
- Hover: `transition: all 200ms ease`
- Active indicator: `transition: box-shadow 300ms ease, transform 200ms ease`
- Collapse/expand: `width 300ms cubic-bezier(0.4, 0, 0.2, 1)`
- Mobile slide: `transform: translateX(-100%) → translateX(0)`, `backdrop-filter: blur(8px)` fade

**Implementation:**
- Blazor component with `@bind-_sidebarOpen` for state
- CSS classes: Enhance existing `.dashboard-theme-sidebar` with new tactical modifiers (`.dashboard-theme-sidebar .cmd-nav-item`, `.cmd-nav-item.active`)
- Add new classes for tactical elements: `.cmd-nav-group`, `.cmd-status-strip`
- Media queries for responsive behavior

### 3.2 Status Bar (`DashboardLayout.razor`)

**Structure:** Full-width bar above main content, height 44px

**Elements (left to right):**
1. **Live Clock:** Monospace, updates every second via `System.Timers.Timer`
2. **Date:** Smaller, lighter text next to clock
3. **System Status:** "SYSTEM STATUS: ONLINE" with pulsing green dot
4. **Scrolling Ticker:** Recent incident summaries scrolling horizontally (CSS marquee)
5. **User Badge:** Avatar (32px) + role pill with colored border glow

**Implementation:**
- Blazor component injected into `DashboardLayout`
- Clock uses `InvokeAsync` + `StateHasChanged` with 1-second timer
- Ticker uses CSS animation, data from `IIncidentService`

### 3.3 Metric Cards (`MetricCard.razor`)

**Structure:** Square-ish card (aspect ratio ~4:3)

**Elements:**
1. **Top Border Glow:** 2px colored line (accent color) with `box-shadow` glow
2. **Icon Container:** Circular with colored glow (preferred — reliable cross-browser). Hexagonal (CSS clip-path) as enhancement if time permits
3. **Value:** Large monospace font (28-36px), animated count-up
4. **Label:** Terminal-style uppercase text below
5. **Status Dot:** Tiny pulsing dot in top-right corner

**Variants:**
- `Primary` (green): Safe metrics
- `Warning` (amber): Caution metrics
- `Danger` (red): Critical metrics
- `Info` (cyan): General metrics
- `Purple` (purple): Authority metrics

**Animations:**
- Load: `opacity: 0 → 1`, `translateY(20px) → 0`, staggered 50ms
- Count-up: 1.5s ease-out from 0
- Hover: `translateY(-4px)`, glow intensifies

### 3.4 Incident Cards (`IncidentCard.razor`)

**Structure:** Horizontal card, 80-100px tall

**Elements:**
1. **Left Accent Bar:** 4px wide, colored by severity (red/orange/amber/green)
2. **Severity Badge:** Icon + text, colored
3. **Title:** Bold, 1-2 lines
4. **Incident Number:** Monospace, copy-on-click
5. **Location:** Small text with pin icon
6. **Status Pill:** Glowing badge (Pending=amber, Assigned=cyan, Resolved=green, Closed=neutral)
7. **Time Ago:** Monospace, small

**Severity Styling:**
- Critical: Pulsing red border glow, urgent feel
- High: Orange border glow
- Medium: Amber border
- Low: Subtle green border

**Hover:** Card lifts 2px, accent bar widens to 6px, action buttons fade in (Edit, View, Resolve)

### 3.5 Modal / Detail View (`Modal.razor` or inline)

**Structure:** Full-screen overlay

**Elements:**
1. **Backdrop:** `rgba(0,0,0,0.7)` + `backdrop-filter: blur(8px)`
2. **Modal Card:** Glass morphism with animated gradient border
3. **Header:** Title + incident number + close button (X with glow)
4. **Body:** "Case File" layout
   - 2-column grid for metadata (Category, Status, Reported, Location)
   - Full-width description area
   - Assigned officer card (if applicable)
   - Resolution banner (if resolved/closed)
5. **Footer:** Action buttons (Resolve, Escalate, Close)

**Animations:**
- Open: Backdrop fades in (200ms), modal scales 0.9→1.0 + fades in (300ms, spring easing)
- Close: Reverse
- Critical modals: Red flash on backdrop

### 3.6 Forms & Inputs (Global)

**Input Style:**
- Background: `--surface`
- Border: Bottom only, 1px `--glass-border`
- Label: Terminal-style uppercase, above input
- Focus: Bottom border slides in from center (CSS animation), glow appears below
- Error: Red border + message slides in from left
- Icon: Left-aligned inside input (if applicable)

**Select/Dropdown:**
- Same base styling
- Options: Glass morphism dropdown
- Selected: Colored left border

**Buttons:**
- Primary: Full `--neon-green` background, black text, glow on hover
- Secondary: Transparent + green border, green text
- Danger: `--alert-red` background, glow on hover
- Ghost: Transparent, text only, underline on hover

**Submit Animation:**
- Click: Ripple from center (CSS `::after` pseudo-element)
- Loading: Text replaced with spinner + "PROCESSING..."
- Success: Green flash + "COMPLETE"
- Error: Red shake + message

### 3.7 Login / Register Pages

**Background:**
- Full-screen Three.js nebula particle field (slower, more ambient than hero)
- Deep purple and cyan particles drifting

**Login Card:**
- Glass morphism with animated gradient border (rotating through green/cyan)
- Terminal-style header: "SECURE ACCESS TERMINAL"
- Inputs: Dark, bottom-border only, blinking cursor on focus
- "AUTHENTICATE" button: Wide, glowing, system-check loading animation
- Success state: "ACCESS GRANTED" + green pulse + redirect

**Register Card:**
- Same styling
- Multi-step form with progress indicator (3 dots, glowing active step)
- "INITIALIZE ACCOUNT" button

### 3.8 Landing Page (`Index.razor`)

**Hero Section:**
- Three.js molecular structure (existing) + slow-rotating orbital rings
- Floating feature cards with hover levitation (translateY -8px)
- Stats: Animated counters with glowing numbers
- "System Online" badge with pulsing dot
- CTA buttons: Gradient glow on hover

**Features Section:**
- 3 feature cards in a row
- Each card has an icon in a glowing hexagon
- Hover: Card lifts, icon rotates slightly, description fades in

**Footer:**
- Minimal, dark
- "SafeZone v1.0.0 — SYSTEM OPERATIONAL"

### 3.9 Loading & Empty States

**Skeleton Loaders:**
- Card-shaped placeholder with shimmer sweep
- 1.5s loop, diagonal gradient

**Spinners:**
- Segmented ring rotating with trailing glow
- 1s rotation, linear

**Empty States:**
- Minimalist icon (line art)
- Message: "NO DATA AVAILABLE" or "SYSTEM READY — AWAITING INPUT"
- CTA: "Initialize" or "Deploy" button

### 3.10 Toast Notifications (`Toast.razor`)

**Structure:** Slide in from top-right

**Elements:**
- Colored left border (4px)
- Icon + message
- Close button
- Thin progress bar at bottom (auto-dismiss countdown)

**Variants:**
- Success: Green border + glow
- Error: Red border + glow
- Info: Cyan border
- Warning: Amber border
- Critical: Red border + pulsing, stays until dismissed

**Animations:**
- Enter: `translateX(100%) → translateX(0)`, 300ms
- Exit: `translateX(0) → translateX(100%)`, 200ms
- Progress bar: `width: 100% → 0%` over auto-dismiss duration

---

## 4. Page-by-Page Implementation

### 4.1 Landing Page (`/`) — `Index.razor`

**Current State:** Good hero with Three.js, feature cards, stats

**Upgrades:**
- [ ] Enhance Three.js scene: add slow-rotating rings around the molecule
- [ ] Add floating animation to feature cards (subtle up/down drift)
- [ ] Animate stat counters with glow effect on complete
- [ ] Add "System Online" badge with pulsing dot
- [ ] CTA buttons: gradient glow border on hover
- [ ] Add scroll-triggered entrance animations for feature cards

### 4.2 Login (`/login`) — `Login.razor`

**Current State:** Centered card on dark background

**Upgrades:**
- [ ] Full-screen Three.js nebula background (new component)
- [ ] Glass card with animated gradient border
- [ ] Terminal-style header: "SECURE ACCESS TERMINAL"
- [ ] Inputs: bottom-border-only, blinking cursor, glow on focus
- [ ] "AUTHENTICATE" button with ripple + loading state
- [ ] Success: "ACCESS GRANTED" + green pulse

### 4.3 Register (`/register`) — `Register.razor`

**Current State:** Standard form layout

**Upgrades:**
- [ ] Same nebula background as login
- [ ] Multi-step form (3 steps: Account → Profile → Confirm)
- [ ] Progress dots with glow on active step
- [ ] Step transitions: slide left/right
- [ ] "INITIALIZE ACCOUNT" button

### 4.4 User Dashboard (`/user/dashboard`) — `Dashboard.razor`

**Current State:** Metric cards, action grid, map, recent incidents

**Upgrades:**
- [ ] Add status bar (clock, ticker, user badge)
- [ ] Redesign metric cards with glowing top border + animated counters
- [ ] Quick action grid: hexagonal icons + hover lift/glow
- [ ] Live map: bouncing markers + heatmap pulse
- [ ] Recent incidents: severity-coded cards with hover actions
- [ ] Add ambient particle canvas (lightweight Three.js)

### 4.5 My Incidents (`/user/my-incidents`) — `MyIncidents.razor`

**Current State:** Filter bar, incident list, detail modal

**Upgrades:**
- [ ] Filter bar: glowing active pills
- [ ] Incident cards: severity-coded with left accent bar
- [ ] Hover: lift + action buttons fade in
- [ ] Modal: holographic overlay + case file layout
- [ ] Status transitions animated in modal

### 4.6 Report Incident (`/user/report-incident`) — `ReportIncident.razor`

**Current State:** Standard form with map picker

**Upgrades:**
- [ ] Form sections as "mission briefing" cards
- [ ] Section headers: holographic style with icon
- [ ] Location picker: live map with pulsing "select location" marker
- [ ] Submit: "DEPLOY REPORT" button with countdown loading
- [ ] Success: Animated confirmation with incident number

### 4.7 My FIRs (`/user/my-firs`) — `MyFirs.razor`

**Current State:** Table/list of FIRs

**Upgrades:**
- [ ] FIR cards similar to incident cards
- [ ] Status tracking: visual stepper (Submitted → Under Review → Approved/Closed)
- [ ] Stepper: glowing active step, completed steps have checkmark

### 4.8 SOS (`/user/sos`) — `Sos.razor`

**Current State:** SOS button + location

**Upgrades:**
- [ ] **Full-screen emergency interface**
- [ ] Giant pulsing red SOS button (heartbeat animation)
- [ ] Countdown after press: "10... 9... 8..." (cancelable)
- [ ] Location auto-detected with pulsing map marker
- [ ] Post-send: "SIGNAL TRANSMITTED" + green pulse + incident number

### 4.9 Notifications (`/user/notifications`) — `Notifications.razor`

**Current State:** List of alerts

**Upgrades:**
- [ ] Notification cards with severity-colored left border
- [ ] Unread: pulsing glow + bold text
- [ ] Mark as read: swipe or click, card dims
- [ ] Empty state: "ALL CLEAR — NO ACTIVE ALERTS"

### 4.10 Profile (`/user/profile`) — `Profile.razor`

**Current State:** Standard profile form

**Upgrades:**
- [ ] Profile card: large avatar with role-colored ring glow
- [ ] Stats: incident count, SOS count, member since
- [ ] Form: terminal-style inputs
- [ ] Save: "UPDATE PROFILE" with loading + success

### 4.11 Settings (`/user/settings`) — `Settings.razor`

**Current State:** Standard settings form

**Upgrades:**
- [ ] Settings as "system configuration" panels
- [ ] Toggle switches: glowing on/off states
- [ ] Danger zone: red border + "CONFIRM" required

### 4.12 Authority Board (`/authority/board`) — `AuthorityBoard.razor`

**Current State:** Stats, action grid, incident list

**Upgrades:**
- [ ] Darker, more intense theme
- [ ] Command Center header with alert section
- [ ] Incident stream: auto-refreshing cards with newest on top
- [ ] Dispatch map: unit markers with status colors
- [ ] Action grid: "Deploy Unit", "Broadcast Alert", "Review FIR"

### 4.13 Dispatch Map (`/authority/dispatch`) — `DispatchMap.razor`

**Current State:** Full Leaflet map

**Upgrades:**
- [ ] Dark-themed Leaflet map (CartoDB Dark Matter or custom)
- [ ] Pulsing incident markers (color-coded by severity)
- [ ] Unit markers: vehicle icons with directional arrows
- [ ] Heatmap overlay: animated pulse
- [ ] Sidebar: active incidents list synced with map

### 4.14 Field Reports (`/authority/field-reports`) — `FieldReports.razor`

**Current State:** Report list

**Upgrades:**
- [ ] Report cards with officer avatar + status
- [ ] Priority indicator: pulsing for urgent
- [ ] Quick actions: "Review", "Assign", "Close"

### 4.15 FIR Management (`/authority/fir-management`) — `FIRManagement.razor`

**Current State:** FIR list

**Upgrades:**
- [ ] FIR table with glowing status pills
- [ ] Approval workflow: stepper with glow
- [ ] Bulk actions: "Approve Selected", "Reject Selected"

### 4.16 Kanban Board (`/authority/kanban`) — `KanbanBoard.razor`

**Current State:** Basic kanban columns

**Upgrades:**
- [ ] Columns: "Incoming", "In Progress", "Resolved", "Closed"
- [ ] Cards: draggable with glow on drag
- [ ] Column headers: colored with incident count badge
- [ ] Drag: card lifts, glows, placeholder appears

### 4.17 SOS Logs (`/authority/sos-logs`) — `SosLogs.razor`

**Current State:** Log table

**Upgrades:**
- [ ] Log entries: time + location + status
- [ ] Urgent entries: red glow + pulsing
- [ ] Map thumbnail: shows SOS location

### 4.18 AI Agent (`/authority/ai-agent`) — `AiAgent.razor`

**Current State:** ElevenLabs widget

**Upgrades:**
- [ ] AI interface styled as "Command AI Terminal"
- [ ] Conversation bubbles with glow
- [ ] "AI STATUS: ONLINE" indicator with pulsing dot
- [ ] Voice waveform animation when speaking

### 4.19 Settings (`/authority/settings`) — `AuthoritySettings.razor`

**Current State:** Standard settings

**Upgrades:**
- [ ] System configuration panels
- [ ] Toggle switches with glow
- [ ] Danger zone: red border + confirm

### 4.20 User Management (`/authority/user-management`) — `UserManagement.razor`

**Current State:** User table

**Upgrades:**
- [ ] User cards: avatar + role badge with colored glow
- [ ] Role dropdown: glowing options
- [ ] Actions: "Edit", "Deactivate" (red), "Promote" (purple)

---

## 5. Animation Specifications

### 5.1 Global Page Transitions

```css
@keyframes page-enter {
  from { opacity: 0; transform: scale(0.98); }
  to { opacity: 1; transform: scale(1); }
}

@keyframes page-exit {
  from { opacity: 1; transform: scale(1); }
  to { opacity: 0; transform: scale(0.98); }
}
```

**Usage:** Apply `page-enter` on `@Body` container when route changes. Blazor `NavigationManager.LocationChanged` event triggers a CSS class toggle.

### 5.2 Card Entrance Stagger

```css
@keyframes card-enter {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
```

**Usage:** Apply to cards on page load. Stagger delay: `animation-delay: calc(var(--index) * 50ms)`.

### 5.3 Glow Pulse

```css
@keyframes pulse-glow {
  0%, 100% { box-shadow: 0 0 5px var(--color-glow); }
  50% { box-shadow: 0 0 20px var(--color-glow), 0 0 40px var(--color-glow); }
}
```

**Usage:** Active status dots, SOS button, critical alerts.

### 5.4 Shimmer

```css
@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}
```

**Usage:** Skeleton loaders. Background: `linear-gradient(90deg, transparent, rgba(255,255,255,0.05), transparent)`.

### 5.5 Gradient Border Rotation

```css
@keyframes border-rotate {
  0% { --angle: 0deg; }
  100% { --angle: 360deg; }
}
```

**Usage:** Login card, active modals. Uses `conic-gradient` with CSS variable `--angle`.

### 5.6 Grid Pan

```css
@keyframes grid-pan {
  0% { background-position: 0 0; }
  100% { background-position: 32px 32px; }
}
```

**Usage:** Animated dot-grid background on dashboards. Very subtle, 20s loop.

### 5.7 Count-Up

**Implementation:** Blazor component or JS interop. Animate number from 0 to target over 1.5s with `easeOutExpo` easing.

### 5.8 Ripple

**Implementation:** On button click, create a `span` at click coordinates, animate `scale(0) → scale(4)` + `opacity(0.5) → opacity(0)`, remove after animation.

---

## 6. Technical Implementation Notes

### 6.1 Performance Budget

- **Target:** 60fps on mid-range laptops
- **Three.js:** Only on landing page + login. Use lightweight `Points` (not complex meshes) for dashboard backgrounds.
- **Animations:** Use CSS `transform` and `opacity` only (GPU-accelerated). Avoid animating `width`, `height`, `margin`, `padding`.
- **Particle count:** Max 200 particles on dashboards.
- **Timers:** Use `System.Timers.Timer` or `PeriodicTimer` with proper disposal. Never use `async void`.
- **JS Interop:** Minimize calls. Batch where possible.

### 6.2 CSS Organization

**Current state:** `global.css` is ~2600 lines. Reorganize into clear sections:

```
global.css:
├── 1. Base & Reset (lines 1-100)
├── 2. Design Tokens — CSS variables (lines 100-200)
├── 3. Global Effects — scan lines, animated grid, ambient glow (lines 200-350)
├── 4. Layout — dashboard shell, sidebar, status bar, modal overlay (lines 350-650)
├── 5. Components — cards, buttons, inputs, nav items, badges (lines 650-1200)
├── 6. Animations — all @keyframes (lines 1200-1400)
├── 7. Utilities — Tailwind-style helpers (lines 1400-1800)
├── 8. Page-specific overrides (lines 1800-2200)
├── 9. Media Queries — responsive rules (lines 2200-2600)
```

**Rule:** Add new tactical styles to appropriate sections. Don't just append to end — maintain organization.

### 6.3 Blazor Component Structure

```
Components/
├── Layout/
│   ├── DashboardLayout.razor       (status bar + sidebar + main)
│   ├── MainLayout.razor            (landing page layout)
│   └── LoginLayout.razor           (auth page layout)
├── Shared/
│   ├── AppSidebar.razor            (command sidebar)
│   ├── StatusBar.razor             (top status bar)
│   ├── MetricCard.razor            (metric card)
│   ├── IncidentCard.razor          (incident card)
│   ├── GlassCard.razor             (generic glass card)
│   ├── PageHeader.razor            (page title + breadcrumb)
│   ├── Toast.razor                 (toast notifications)
│   ├── ConfirmDialog.razor         (confirm modal)
│   ├── LoadingSpinner.razor        (spinner)
│   ├── LoadingSkeleton.razor       (skeleton loader)
│   ├── EmptyState.razor            (empty state)
│   ├── StatusChip.razor            (status badge)
│   ├── SeverityChip.razor          (severity badge)
│   └── Badge.razor                 (generic badge)
```

### 6.4 State Management

- Use Blazor's built-in component state
- `NavigationManager.LocationChanged` for page transition animations
- `IJSRuntime` for complex animations (Three.js, ripple effects)
- `Timer`/`PeriodicTimer` for clock, ticker, auto-refresh

### 6.5 Accessibility

- **Motion:** Respect `prefers-reduced-motion` media query. Disable animations for users who prefer reduced motion.
- **Contrast:** Ensure all text meets WCAG AA contrast ratios (4.5:1 for normal text, 3:1 for large text).
- **Focus:** Visible focus indicators (glowing outline) on all interactive elements.
- **Screen readers:** Proper ARIA labels, roles, and live regions for status updates.

---

## 7. Implementation Phases

### Phase 1: Foundation (Day 1-2)
- [ ] Update CSS variables and base styles
- [ ] Add global animations (keyframes)
- [ ] Add scan lines + animated grid textures
- [ ] Update layout components (sidebar, status bar)

### Phase 2: Components (Day 3-4)
- [ ] Redesign shared components (cards, buttons, inputs, modals)
- [ ] Add Toast, LoadingSpinner, Skeleton, EmptyState
- [ ] Add StatusBar component

### Phase 3: Pages (Day 5-7)
- [ ] Landing page enhancements
- [ ] Login/Register redesign
- [ ] User dashboard redesign
- [ ] My Incidents + modal redesign
- [ ] Report Incident redesign

### Phase 4: Authority (Day 8-9)
- [ ] Authority board redesign
- [ ] Dispatch map enhancements
- [ ] Kanban board enhancements
- [ ] All authority pages

### Phase 5: Polish (Day 10)
- [ ] Performance optimization
- [ ] Reduced motion support
- [ ] Final testing + bug fixes

---

## 8. Dependencies

**Existing (keep):**
- Tailwind CSS (CDN)
- Three.js (CDN)
- Leaflet.js
- Blazor Server

**New (add if needed):**
- No new major dependencies
- All effects achieved with CSS + existing Three.js

---

## 9. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Performance on low-end devices | `prefers-reduced-motion` disables animations; Three.js scenes are lightweight |
| Blazor Server latency affecting animations | Use CSS animations (client-side), not JS-driven |
| CSS file becomes too large | Split into component-scoped styles if needed; current ~2600 lines is manageable |
| Overwhelming visual design | Keep information density high but readable; user testing |

---

## 10. Acceptance Criteria

- [ ] All 20+ pages have consistent tactical command center aesthetic
- [ ] Animations run at 60fps on mid-range hardware
- [ ] `prefers-reduced-motion` fully disables animations
- [ ] No functional regressions (all features still work)
- [ ] Build succeeds with zero errors
- [ ] App runs smoothly on `http://localhost:5002`

---

**Next Step:** After approval, create implementation plan using `writing-plans` skill.
