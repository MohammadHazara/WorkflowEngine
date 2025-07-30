/**
 * Job Management Web Interface
 * Handles all client-side functionality for managing jobs and tasks
 */

// Global state
let currentJobGroups = [];
let currentTaskTemplates = [];
let selectedTasks = [];
let currentSelectedJob = null;

// API base URL
const API_BASE = '/api/JobManagement';

/**
 * Initialize the application when DOM is loaded
 */
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
});

/**
 * Initializes the application by loading initial data
 */
async function initializeApp() {
    showLoading(true);
    try {
        await Promise.all([
            loadJobGroups(),
            loadTaskTemplates()
        ]);
        updateStatistics();
    } catch (error) {
        showNotification('Error loading application data', 'error');
        console.error('Initialization error:', error);
    } finally {
        showLoading(false);
    }
}

/**
 * Shows or hides the loading spinner
 * @param {boolean} show - Whether to show the spinner
 */
function showLoading(show) {
    const spinner = document.getElementById('loadingSpinner');
    const container = document.getElementById('jobGroupsContainer');
    
    if (show) {
        spinner.style.display = 'block';
        container.style.display = 'none';
    } else {
        spinner.style.display = 'none';
        container.style.display = 'block';
    }
}

/**
 * Loads job groups from the API and renders them
 */
async function loadJobGroups() {
    try {
        const response = await fetch(`${API_BASE}/job-groups?includeJobs=true`);
        if (!response.ok) throw new Error('Failed to load job groups');
        
        const data = await response.json();
        currentJobGroups = data.jobGroups || [];
        
        renderJobGroups();
        updateJobGroupFilters();
    } catch (error) {
        console.error('Error loading job groups:', error);
        throw error;
    }
}

/**
 * Loads task templates from the API
 */
async function loadTaskTemplates() {
    try {
        const response = await fetch(`${API_BASE}/task-templates`);
        if (!response.ok) throw new Error('Failed to load task templates');
        
        currentTaskTemplates = await response.json();
    } catch (error) {
        console.error('Error loading task templates:', error);
        throw error;
    }
}

/**
 * Renders job groups in the main container
 */
function renderJobGroups() {
    const container = document.getElementById('jobGroupsContainer');
    
    if (currentJobGroups.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="fas fa-folder-open fa-3x text-muted mb-3"></i>
                <h5>No Job Groups Found</h5>
                <p class="text-muted">Create your first job group to get started</p>
                <button class="btn btn-primary" onclick="showCreateJobGroupModal()">
                    <i class="fas fa-plus"></i> Create Job Group
                </button>
            </div>
        `;
        return;
    }

    container.innerHTML = currentJobGroups.map(group => `
        <div class="card mb-4 fade-in">
            <div class="card-header d-flex justify-content-between align-items-center">
                <div>
                    <h5 class="mb-0">
                        <i class="fas fa-folder text-primary me-2"></i>
                        ${escapeHtml(group.name)}
                    </h5>
                    ${group.description ? `<small class="text-muted">${escapeHtml(group.description)}</small>` : ''}
                </div>
                <div>
                    <span class="badge bg-info">${group.jobs.length} jobs</span>
                    <small class="text-muted ms-2">${formatDate(group.createdAt)}</small>
                </div>
            </div>
            <div class="card-body">
                ${renderJobsForGroup(group)}
            </div>
        </div>
    `).join('');
}

/**
 * Renders jobs for a specific group
 * @param {Object} group - The job group object
 * @returns {string} HTML string for jobs
 */
function renderJobsForGroup(group) {
    if (group.jobs.length === 0) {
        return `
            <div class="text-center py-3">
                <p class="text-muted mb-3">No jobs in this group yet</p>
                <button class="btn btn-sm btn-outline-primary" onclick="showCreateJobModal(${group.id})">
                    <i class="fas fa-plus"></i> Add Job
                </button>
            </div>
        `;
    }

    return `
        <div class="row">
            ${group.jobs.map(job => `
                <div class="col-md-6 col-lg-4 mb-3">
                    <div class="card job-card h-100" onclick="showJobDetails(${job.id})">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h6 class="card-title mb-0">${escapeHtml(job.name)}</h6>
                                <span class="badge bg-secondary">${job.jobType}</span>
                            </div>
                            ${job.description ? `<p class="card-text text-muted small">${escapeHtml(job.description)}</p>` : ''}
                            
                            <div class="mb-2">
                                <small class="text-muted">
                                    <i class="fas fa-tasks"></i> ${job.tasks.length} tasks
                                    <span class="ms-2">
                                        <i class="fas fa-sort-numeric-up"></i> Order: ${job.executionOrder}
                                    </span>
                                </small>
                            </div>

                            ${renderJobExecutionStatus(job)}
                            
                            <div class="mt-2">
                                ${renderJobTasks(job.tasks.slice(0, 3))}
                                ${job.tasks.length > 3 ? `<small class="text-muted">+${job.tasks.length - 3} more tasks</small>` : ''}
                            </div>
                        </div>
                        <div class="card-footer">
                            <div class="d-flex justify-content-between align-items-center">
                                <small class="text-muted">${formatDate(job.updatedAt)}</small>
                                <div>
                                    <button class="btn btn-sm btn-outline-success" onclick="executeJob(${job.id}); event.stopPropagation();">
                                        <i class="fas fa-play"></i>
                                    </button>
                                    <button class="btn btn-sm btn-outline-primary" onclick="showJobDetails(${job.id}); event.stopPropagation();">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Renders execution status for a job
 * @param {Object} job - The job object
 * @returns {string} HTML string for execution status
 */
function renderJobExecutionStatus(job) {
    if (!job.recentExecutions || job.recentExecutions.length === 0) {
        return '<small class="text-muted">No executions yet</small>';
    }

    const lastExecution = job.recentExecutions[0];
    const statusClass = getStatusClass(lastExecution.status);
    
    return `
        <div class="mb-2">
            <div class="d-flex justify-content-between align-items-center">
                <span class="badge ${statusClass} status-badge">${lastExecution.status}</span>
                <small class="text-muted">${lastExecution.progressPercentage}%</small>
            </div>
            <div class="execution-progress mt-1">
                <div class="execution-progress-bar" style="width: ${lastExecution.progressPercentage}%"></div>
            </div>
        </div>
    `;
}

/**
 * Renders task list for a job
 * @param {Array} tasks - Array of tasks
 * @returns {string} HTML string for tasks
 */
function renderJobTasks(tasks) {
    return tasks.map(task => `
        <div class="task-item ${getTaskClass(task.taskType)} p-2 rounded mb-1">
            <small>
                <strong>${task.executionOrder}.</strong> ${escapeHtml(task.name)}
                <span class="badge bg-light text-dark ms-1">${task.taskType}</span>
            </small>
        </div>
    `).join('');
}

/**
 * Gets CSS class for task type
 * @param {string} taskType - The task type
 * @returns {string} CSS class name
 */
function getTaskClass(taskType) {
    const classMap = {
        'FetchApiData': 'task-fetch',
        'CreateFile': 'task-file',
        'CompressFile': 'task-compress',
        'UploadSftp': 'task-sftp'
    };
    return classMap[taskType] || '';
}

/**
 * Gets CSS class for execution status
 * @param {string} status - The execution status
 * @returns {string} CSS class name
 */
function getStatusClass(status) {
    const classMap = {
        'Completed': 'bg-success',
        'Running': 'bg-warning',
        'Failed': 'bg-danger',
        'Pending': 'bg-secondary',
        'Cancelled': 'bg-dark'
    };
    return classMap[status] || 'bg-secondary';
}

/**
 * Updates the job group filter dropdown
 */
function updateJobGroupFilters() {
    const filter = document.getElementById('jobGroupFilter');
    const jobGroupSelect = document.getElementById('jobGroup');
    
    const groupOptions = currentJobGroups.map(group => 
        `<option value="${group.id}">${escapeHtml(group.name)}</option>`
    ).join('');
    
    filter.innerHTML = '<option value="">All Groups</option>' + groupOptions;
    if (jobGroupSelect) {
        jobGroupSelect.innerHTML = '<option value="">Select a job group</option>' + groupOptions;
    }
}

/**
 * Updates statistics in the sidebar
 */
function updateStatistics() {
    const totalJobGroups = currentJobGroups.length;
    const totalJobs = currentJobGroups.reduce((sum, group) => sum + group.jobs.length, 0);
    const runningJobs = currentJobGroups
        .flatMap(group => group.jobs)
        .filter(job => job.recentExecutions && job.recentExecutions.some(exec => exec.status === 'Running'))
        .length;

    document.getElementById('totalJobGroups').textContent = totalJobGroups;
    document.getElementById('totalJobs').textContent = totalJobs;
    document.getElementById('runningJobs').textContent = runningJobs;
}

/**
 * Shows the create job group modal
 */
function showCreateJobGroupModal() {
    // Reset form
    document.getElementById('createJobGroupForm').reset();
    
    const modal = new bootstrap.Modal(document.getElementById('createJobGroupModal'));
    modal.show();
}

/**
 * Creates a new job group
 */
async function createJobGroup() {
    const name = document.getElementById('jobGroupName').value.trim();
    const description = document.getElementById('jobGroupDescription').value.trim();

    if (!name) {
        showNotification('Please enter a job group name', 'error');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/job-groups`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: name,
                description: description || null
            })
        });

        if (!response.ok) {
            throw new Error('Failed to create job group');
        }

        showNotification('Job group created successfully', 'success');
        bootstrap.Modal.getInstance(document.getElementById('createJobGroupModal')).hide();
        
        await refreshData();
    } catch (error) {
        console.error('Error creating job group:', error);
        showNotification('Error creating job group', 'error');
    }
}

/**
 * Shows the create job modal
 * @param {number} groupId - Optional group ID to pre-select
 */
function showCreateJobModal(groupId = null) {
    // Reset form and selected tasks
    document.getElementById('createJobForm').reset();
    selectedTasks = [];
    updateSelectedTasksDisplay();

    // Pre-select group if provided
    if (groupId) {
        document.getElementById('jobGroup').value = groupId;
    }

    const modal = new bootstrap.Modal(document.getElementById('createJobModal'));
    modal.show();
}

/**
 * Shows the add task modal
 */
function showAddTaskModal() {
    // Reset task form
    resetTaskForm();
    
    // Load task templates
    renderTaskTemplates();
    
    const modal = new bootstrap.Modal(document.getElementById('addTaskModal'));
    modal.show();
}

/**
 * Renders task templates in the modal
 */
function renderTaskTemplates() {
    const container = document.getElementById('taskTemplates');
    
    container.innerHTML = currentTaskTemplates.map(template => `
        <div class="card task-template mb-2" onclick="selectTaskTemplate('${template.taskType}')">
            <div class="card-body p-3">
                <h6 class="card-title mb-1">${escapeHtml(template.name)}</h6>
                <p class="card-text small text-muted">${escapeHtml(template.description)}</p>
                <span class="badge bg-primary">${template.taskType}</span>
            </div>
        </div>
    `).join('');
}

/**
 * Selects a task template and shows configuration
 * @param {string} taskType - The task type to select
 */
function selectTaskTemplate(taskType) {
    // Remove previous selections
    document.querySelectorAll('.task-template').forEach(el => {
        el.classList.remove('selected');
    });

    // Select current template
    event.target.closest('.task-template').classList.add('selected');

    const template = currentTaskTemplates.find(t => t.taskType === taskType);
    if (!template) return;

    // Show configuration section
    document.getElementById('taskConfigSection').style.display = 'block';
    document.getElementById('selectTaskPrompt').style.display = 'none';
    document.getElementById('addTaskBtn').style.display = 'block';

    // Set default values
    document.getElementById('taskName').value = template.name;
    document.getElementById('taskDescription').value = template.description;
    document.getElementById('taskExecutionOrder').value = selectedTasks.length + 1;

    // Parse and display configuration schema
    try {
        const schema = JSON.parse(template.configurationSchema);
        const defaultConfig = {};
        
        if (schema.properties) {
            Object.keys(schema.properties).forEach(key => {
                const prop = schema.properties[key];
                if (prop.default !== undefined) {
                    defaultConfig[key] = prop.default;
                }
            });
        }

        document.getElementById('taskConfig').value = JSON.stringify(defaultConfig, null, 2);
    } catch (error) {
        document.getElementById('taskConfig').value = '{}';
    }

    // Store current template
    window.currentTaskTemplate = template;
}

/**
 * Adds the configured task to the job
 */
function addTaskToJob() {
    const name = document.getElementById('taskName').value.trim();
    const description = document.getElementById('taskDescription').value.trim();
    const executionOrder = parseInt(document.getElementById('taskExecutionOrder').value);
    const timeout = parseInt(document.getElementById('taskTimeout').value);
    const configData = document.getElementById('taskConfig').value.trim();

    if (!name || !window.currentTaskTemplate) {
        showNotification('Please fill in all required fields', 'error');
        return;
    }

    // Validate JSON configuration
    try {
        JSON.parse(configData);
    } catch (error) {
        showNotification('Invalid JSON configuration', 'error');
        return;
    }

    // Create task object
    const task = {
        id: selectedTasks.length + 1,
        name: name,
        description: description,
        taskType: window.currentTaskTemplate.taskType,
        executionOrder: executionOrder,
        configurationData: configData,
        maxRetries: 3,
        timeoutSeconds: timeout
    };

    selectedTasks.push(task);
    updateSelectedTasksDisplay();

    // Hide modal
    bootstrap.Modal.getInstance(document.getElementById('addTaskModal')).hide();
    showNotification('Task added successfully', 'success');
}

/**
 * Updates the selected tasks display in the create job modal
 */
function updateSelectedTasksDisplay() {
    const container = document.getElementById('selectedTasks');
    
    if (selectedTasks.length === 0) {
        container.innerHTML = '<p class="text-muted text-center mb-0">No tasks added yet. Click "Add Task" to get started.</p>';
        return;
    }

    container.innerHTML = selectedTasks
        .sort((a, b) => a.executionOrder - b.executionOrder)
        .map((task, index) => `
            <div class="task-item ${getTaskClass(task.taskType)} p-3 rounded mb-2">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <h6 class="mb-1">
                            <span class="badge bg-primary me-2">${task.executionOrder}</span>
                            ${escapeHtml(task.name)}
                            <span class="badge bg-secondary ms-2">${task.taskType}</span>
                        </h6>
                        ${task.description ? `<p class="text-muted small mb-2">${escapeHtml(task.description)}</p>` : ''}
                        <small class="text-muted">
                            Timeout: ${task.timeoutSeconds}s | 
                            Config: ${task.configurationData ? 'Configured' : 'Default'}
                        </small>
                    </div>
                    <button class="btn btn-sm btn-outline-danger" onclick="removeTask(${index})">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
}

/**
 * Removes a task from the selected tasks
 * @param {number} index - The task index to remove
 */
function removeTask(index) {
    selectedTasks.splice(index, 1);
    updateSelectedTasksDisplay();
}

/**
 * Creates a new job with selected tasks
 */
async function createJob() {
    const name = document.getElementById('jobName').value.trim();
    const description = document.getElementById('jobDescription').value.trim();
    const jobType = document.getElementById('jobType').value;
    const jobGroupId = parseInt(document.getElementById('jobGroup').value);
    const executionOrder = parseInt(document.getElementById('executionOrder').value);

    if (!name || !jobGroupId) {
        showNotification('Please fill in all required fields', 'error');
        return;
    }

    if (selectedTasks.length === 0) {
        showNotification('Please add at least one task to the job', 'error');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/jobs`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: name,
                description: description || null,
                jobType: jobType,
                jobGroupId: jobGroupId,
                executionOrder: executionOrder,
                tasks: selectedTasks
            })
        });

        if (!response.ok) {
            throw new Error('Failed to create job');
        }

        showNotification('Job created successfully', 'success');
        bootstrap.Modal.getInstance(document.getElementById('createJobModal')).hide();
        
        await refreshData();
    } catch (error) {
        console.error('Error creating job:', error);
        showNotification('Error creating job', 'error');
    }
}

/**
 * Shows detailed view of a job
 * @param {number} jobId - The job ID to show details for
 */
async function showJobDetails(jobId) {
    try {
        const response = await fetch(`${API_BASE}/jobs/${jobId}`);
        if (!response.ok) throw new Error('Failed to load job details');
        
        const job = await response.json();
        currentSelectedJob = job;
        
        renderJobDetails(job);
        
        const modal = new bootstrap.Modal(document.getElementById('jobDetailsModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading job details:', error);
        showNotification('Error loading job details', 'error');
    }
}

/**
 * Renders job details in the modal
 * @param {Object} job - The job object
 */
function renderJobDetails(job) {
    document.getElementById('jobDetailsTitle').textContent = job.name;
    
    const content = document.getElementById('jobDetailsContent');
    content.innerHTML = `
        <div class="row">
            <div class="col-md-6">
                <h6>Job Information</h6>
                <table class="table table-sm">
                    <tr><td><strong>Name:</strong></td><td>${escapeHtml(job.name)}</td></tr>
                    <tr><td><strong>Type:</strong></td><td><span class="badge bg-secondary">${job.jobType}</span></td></tr>
                    <tr><td><strong>Execution Order:</strong></td><td>${job.executionOrder}</td></tr>
                    <tr><td><strong>Created:</strong></td><td>${formatDate(job.createdAt)}</td></tr>
                    <tr><td><strong>Updated:</strong></td><td>${formatDate(job.updatedAt)}</td></tr>
                    ${job.description ? `<tr><td><strong>Description:</strong></td><td>${escapeHtml(job.description)}</td></tr>` : ''}
                </table>
            </div>
            <div class="col-md-6">
                <h6>Recent Executions</h6>
                ${renderExecutionHistory(job.recentExecutions)}
            </div>
        </div>
        
        <div class="mt-4">
            <h6>Tasks (${job.tasks.length})</h6>
            ${renderDetailedTasks(job.tasks)}
        </div>
    `;
}

/**
 * Renders execution history for a job
 * @param {Array} executions - Array of execution objects
 * @returns {string} HTML string for execution history
 */
function renderExecutionHistory(executions) {
    if (!executions || executions.length === 0) {
        return '<p class="text-muted">No executions yet</p>';
    }

    return `
        <div class="list-group">
            ${executions.slice(0, 5).map(exec => `
                <div class="list-group-item">
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="badge ${getStatusClass(exec.status)}">${exec.status}</span>
                        <small>${formatDate(exec.startedAt)}</small>
                    </div>
                    <div class="progress mt-2" style="height: 4px;">
                        <div class="progress-bar" style="width: ${exec.progressPercentage}%"></div>
                    </div>
                    ${exec.durationMs ? `<small class="text-muted">Duration: ${exec.durationMs}ms</small>` : ''}
                    ${exec.errorMessage ? `<small class="text-danger d-block">${escapeHtml(exec.errorMessage)}</small>` : ''}
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Renders detailed task list
 * @param {Array} tasks - Array of task objects
 * @returns {string} HTML string for detailed tasks
 */
function renderDetailedTasks(tasks) {
    return `
        <div class="accordion" id="tasksAccordion">
            ${tasks
                .sort((a, b) => a.executionOrder - b.executionOrder)
                .map((task, index) => `
                    <div class="accordion-item">
                        <h6 class="accordion-header">
                            <button class="accordion-button ${index === 0 ? '' : 'collapsed'}" type="button" 
                                    data-bs-toggle="collapse" data-bs-target="#task${task.id}">
                                <span class="badge bg-primary me-2">${task.executionOrder}</span>
                                ${escapeHtml(task.name)}
                                <span class="badge bg-secondary ms-2">${task.taskType}</span>
                            </button>
                        </h6>
                        <div id="task${task.id}" class="accordion-collapse collapse ${index === 0 ? 'show' : ''}"
                             data-bs-parent="#tasksAccordion">
                            <div class="accordion-body">
                                ${task.description ? `<p><strong>Description:</strong> ${escapeHtml(task.description)}</p>` : ''}
                                <p><strong>Timeout:</strong> ${task.timeoutSeconds} seconds</p>
                                <p><strong>Max Retries:</strong> ${task.maxRetries}</p>
                                ${task.configurationData ? `
                                    <p><strong>Configuration:</strong></p>
                                    <pre class="bg-light p-2 rounded"><code>${escapeHtml(task.configurationData)}</code></pre>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                `).join('')}
        </div>
    `;
}

/**
 * Executes a job
 * @param {number} jobId - The job ID to execute
 */
async function executeJob(jobId) {
    try {
        showNotification('Starting job execution...', 'info');
        
        const response = await fetch(`${API_BASE}/jobs/${jobId}/execute`, {
            method: 'POST'
        });

        if (!response.ok) throw new Error('Failed to execute job');
        
        const execution = await response.json();
        showNotification(`Job execution started (ID: ${execution.id})`, 'success');
        
        // Refresh data to show updated status
        setTimeout(refreshData, 1000);
    } catch (error) {
        console.error('Error executing job:', error);
        showNotification('Error executing job', 'error');
    }
}

/**
 * Executes the currently selected job in the details modal
 */
function executeCurrentJob() {
    if (currentSelectedJob) {
        executeJob(currentSelectedJob.id);
    }
}

/**
 * Refreshes all data
 */
async function refreshData() {
    await loadJobGroups();
    updateStatistics();
}

/**
 * Resets the task form in the add task modal
 */
function resetTaskForm() {
    document.getElementById('taskConfigSection').style.display = 'none';
    document.getElementById('selectTaskPrompt').style.display = 'block';
    document.getElementById('addTaskBtn').style.display = 'none';
    
    // Remove selections
    document.querySelectorAll('.task-template').forEach(el => {
        el.classList.remove('selected');
    });
    
    window.currentTaskTemplate = null;
}

/**
 * Shows a notification toast
 * @param {string} message - The message to show
 * @param {string} type - The notification type (success, error, info)
 */
function showNotification(message, type = 'info') {
    const toast = document.getElementById('notificationToast');
    const messageEl = document.getElementById('toastMessage');
    const headerIcon = toast.querySelector('.toast-header i');
    
    messageEl.textContent = message;
    
    // Update icon and color based on type
    headerIcon.className = `fas me-2 ${type === 'success' ? 'fa-check-circle text-success' : 
                                      type === 'error' ? 'fa-exclamation-circle text-danger' : 
                                      'fa-info-circle text-primary'}`;
    
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
}

/**
 * Escapes HTML characters to prevent XSS
 * @param {string} text - The text to escape
 * @returns {string} Escaped text
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Formats a date string for display
 * @param {string} dateString - The date string to format
 * @returns {string} Formatted date
 */
function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Event listeners for filter changes
document.addEventListener('DOMContentLoaded', function() {
    const jobGroupFilter = document.getElementById('jobGroupFilter');
    if (jobGroupFilter) {
        jobGroupFilter.addEventListener('change', function() {
            // TODO: Implement filtering functionality
            console.log('Filter changed to:', this.value);
        });
    }
});