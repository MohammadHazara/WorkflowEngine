# Dashboard Overview

The WorkflowEngine dashboard provides a comprehensive web-based interface for monitoring and managing workflows in real-time. Built with Bootstrap 5 and modern web technologies, it offers an intuitive experience for workflow operations.

## Dashboard Layout

### Header Navigation
- **Application Title**: WorkflowEngine Dashboard with gear icon
- **Refresh Button**: Manual refresh with spinning animation
- **Real-time Updates**: Auto-refresh every 30 seconds

### Statistics Cards Row
Four key metric cards displaying:
1. **Total Workflows**: Overall workflow count with project diagram icon
2. **Active Workflows**: Currently enabled workflows with play circle icon
3. **Running Executions**: Live execution count with clock icon
4. **Success Rate**: Percentage with color-coded status

### Main Content Area
- **Workflows Panel** (Left, 8/12 width): Workflow management interface
- **Recent Executions Panel** (Right, 4/12 width): Live execution feed

## Statistics Cards

### Total Workflows
- **Purpose**: Shows the complete count of workflows in the system
- **Color**: Blue theme (`border-primary`, `text-primary`)
- **Icon**: Project diagram (`fas fa-project-diagram`)
- **Data Source**: `DashboardStats.TotalWorkflows`

### Active Workflows
- **Purpose**: Displays workflows currently enabled for execution
- **Color**: Green theme (`border-success`, `text-success`)
- **Icon**: Play circle (`fas fa-play-circle`)
- **Data Source**: `DashboardStats.ActiveWorkflows`

### Running Executions
- **Purpose**: Real-time count of workflows currently executing
- **Color**: Info blue theme (`border-info`, `text-info`)
- **Icon**: Clock (`fas fa-clock`)
- **Data Source**: `DashboardStats.RunningExecutions`

### Success Rate
- **Purpose**: Percentage of successfully completed executions
- **Color**: Warning orange theme (`border-warning`, `text-warning`)
- **Icon**: Percentage (`fas fa-percentage`)
- **Data Source**: `DashboardStats.SuccessRate`
- **Format**: Displayed as percentage (e.g., "85.5%")

## Workflows Management Panel

### Header Section
- **Title**: "Workflows" with list icon
- **Create Button**: Primary button to add new workflows
- **Actions**: Create, edit, delete, and execute operations

### Workflow List
Each workflow displays:
- **Name**: Clickable workflow title
- **Description**: Brief workflow description
- **Status Badge**: Active/Inactive indicator
- **Created Date**: Formatted creation timestamp
- **Action Buttons**:
  - **Execute** (Play icon): Start workflow execution
  - **History** (History icon): View execution history
  - **Edit** (Edit icon): Modify workflow properties
  - **Delete** (Trash icon): Remove workflow

### Pagination
- **Navigation**: Previous/Next buttons
- **Page Info**: "Page X of Y" display
- **Page Size**: Configurable items per page (default: 10)

## Recent Executions Panel

### Live Execution Feed
Displays recent workflow executions with:
- **Workflow Name**: Associated workflow title
- **Status Badge**: Color-coded execution status
- **Progress Bar**: Visual execution progress (0-100%)
- **Timestamp**: Start time with relative formatting
- **Duration**: Execution time for completed workflows
- **Error Display**: Error messages for failed executions

### Status Indicators
- 🔵 **Pending**: Waiting to start (blue badge)
- 🟡 **Running**: Currently executing (yellow badge with progress)
- 🟢 **Completed**: Successfully finished (green badge)
- 🔴 **Failed**: Execution failed (red badge with error)
- ⚫ **Cancelled**: Manually cancelled (dark badge)

### Progress Visualization
- **Progress Bars**: Bootstrap progress components
- **Animated**: Running executions show animated progress
- **Color Coded**: Status-based color scheme
- **Percentage**: Numeric progress display

## Interactive Features

### Create Workflow Modal
- **Trigger**: "Create Workflow" button
- **Form Fields**:
  - Name (required, max 255 characters)
  - Description (optional, max 1000 characters)
  - Active checkbox (default: checked)
- **Validation**: Client and server-side validation
- **Submission**: AJAX POST to `/api/workflows`

### Workflow Actions

#### Execute Workflow
- **Button**: Play icon in workflow row
- **Action**: POST to `/api/workflows/{id}/execute`
- **Feedback**: Immediate UI update and success notification
- **Result**: New execution appears in Recent Executions

#### View Execution History
- **Button**: History icon in workflow row
- **Action**: Navigate to workflow execution history
- **Display**: Paginated list of past executions
- **Details**: Full execution information and error logs

#### Edit Workflow
- **Button**: Edit icon in workflow row
- **Modal**: Pre-populated form with current values
- **Action**: PUT to `/api/workflows/{id}`
- **Update**: Immediate UI refresh with updated data

#### Delete Workflow
- **Button**: Trash icon in workflow row
- **Confirmation**: JavaScript confirmation dialog
- **Action**: DELETE to `/api/workflows/{id}`
- **Cascade**: Removes all associated executions

### Cancel Execution
- **Button**: Stop icon in execution row (running only)
- **Action**: POST to `/api/executions/{id}/cancel`
- **Effect**: Graceful execution termination
- **Status**: Updates to "Cancelled" with timestamp

## Real-time Updates

### Auto-refresh Mechanism
- **Interval**: 30-second automatic refresh
- **Components**: Statistics cards and execution list
- **API Calls**:
  - `GET /api/dashboard/stats`
  - `GET /api/dashboard/recent-executions`
- **Preservation**: Form states and user interactions preserved

### Manual Refresh
- **Button**: Refresh icon in navigation
- **Animation**: Spinning icon during update
- **Immediate**: Instant data refresh on demand
- **Status**: Visual feedback for successful updates

## Responsive Design

### Desktop Layout
- **Grid**: Bootstrap 12-column responsive grid
- **Workflows**: 8-column width for main content
- **Executions**: 4-column width for sidebar
- **Cards**: 3-column width for statistics (4 per row)

### Tablet Layout
- **Cards**: 6-column width (2 per row)
- **Panels**: Stacked layout for better readability
- **Navigation**: Compact header with responsive menu

### Mobile Layout
- **Cards**: 12-column width (1 per row)
- **Panels**: Full-width stacked layout
- **Tables**: Horizontal scrolling for workflow lists
- **Touch**: Optimized touch targets for actions

## Performance Optimizations

### Data Loading
- **Pagination**: Efficient large dataset handling
- **Lazy Loading**: On-demand execution history loading
- **Caching**: Browser-level caching for static assets
- **Debouncing**: Prevents excessive API calls

### UI Responsiveness
- **Progressive Enhancement**: Works without JavaScript
- **Loading States**: Spinners and skeleton screens
- **Error Handling**: Graceful degradation for API failures
- **Offline**: Service worker for basic offline functionality

## Accessibility Features

### Screen Reader Support
- **ARIA Labels**: Descriptive labels for interactive elements
- **Semantic HTML**: Proper heading hierarchy and landmarks
- **Alt Text**: Descriptive text for icons and images
- **Focus Management**: Logical tab order and focus indicators

### Keyboard Navigation
- **Tab Order**: Logical navigation sequence
- **Shortcuts**: Keyboard shortcuts for common actions
- **Modal Focus**: Trapped focus in modal dialogs
- **Skip Links**: Quick navigation to main content

### Visual Accessibility
- **Color Contrast**: WCAG AA compliant color ratios
- **Font Sizes**: Scalable text for different viewing needs
- **Icons**: Meaningful icons with text labels
- **Status**: Multiple status indicators (color + text + icons)

## Browser Compatibility

### Supported Browsers
- **Chrome**: Version 90+ (recommended)
- **Firefox**: Version 88+
- **Safari**: Version 14+
- **Edge**: Version 90+

### Feature Requirements
- **ES6+**: Modern JavaScript features
- **Fetch API**: XMLHttpRequest fallback available
- **CSS Grid**: Bootstrap flexbox fallback
- **Local Storage**: Session persistence

## Customization

### Theme Customization
- **Bootstrap Variables**: CSS custom properties
- **Color Scheme**: Configurable brand colors
- **Typography**: Custom font family and sizing
- **Spacing**: Adjustable margin and padding scales

### Layout Options
- **Panel Widths**: Configurable column ratios
- **Card Layout**: Alternative statistics arrangements
- **Navigation**: Collapsible or fixed header options
- **Density**: Compact or comfortable spacing modes

---

The dashboard provides a powerful, user-friendly interface for workflow management with real-time monitoring capabilities, making it easy to oversee complex workflow operations at scale.