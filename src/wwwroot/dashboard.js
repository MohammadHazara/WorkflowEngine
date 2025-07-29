// Dashboard JavaScript functionality
class WorkflowDashboard {
    constructor() {
        this.baseUrl = '/api';
        this.currentPage = 1;
        this.pageSize = 10;
        this.refreshInterval = null;
        this.init();
    }

    async init() {
        await this.loadDashboardData();
        this.startAutoRefresh();
    }

    async loadDashboardData() {
        try {
            await Promise.all([
                this.loadStats(),
                this.loadWorkflows(),
                this.loadRecentExecutions()
            ]);
        } catch (error) {
            console.error('Error loading dashboard data:', error);
            this.showError('Failed to load dashboard data');
        }
    }

    async loadStats() {
        try {
            const response = await fetch(`${this.baseUrl}/dashboard/stats`);
            const stats = await response.json();
            
            document.getElementById('total-workflows').textContent = stats.totalWorkflows;
            document.getElementById('active-workflows').textContent = stats.activeWorkflows;
            document.getElementById('running-executions').textContent = stats.runningExecutions;
            document.getElementById('success-rate').textContent = `${stats.successRate}%`;
        } catch (error) {
            console.error('Error loading stats:', error);
        }
    }

    async loadWorkflows(page = 1) {
        try {
            const response = await fetch(`${this.baseUrl}/workflows?page=${page}&pageSize=${this.pageSize}`);
            const data = await response.json();
            
            this.renderWorkflows(data.items);
            this.renderPagination(data, 'workflows-pagination', (p) => this.loadWorkflows(p));
            this.currentPage = page;
        } catch (error) {
            console.error('Error loading workflows:', error);
            document.getElementById('workflows-list').innerHTML = 
                '<div class="alert alert-danger">Failed to load workflows</div>';
        }
    }

    async loadRecentExecutions() {
        try {
            const response = await fetch(`${this.baseUrl}/dashboard/recent-executions?limit=20`);
            const executions = await response.json();
            
            this.renderRecentExecutions(executions);
        } catch (error) {
            console.error('Error loading recent executions:', error);
            document.getElementById('recent-executions').innerHTML = 
                '<div class="alert alert-danger">Failed to load executions</div>';
        }
    }

    renderWorkflows(workflows) {
        const container = document.getElementById('workflows-list');
        
        if (workflows.length === 0) {
            container.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-inbox fa-3x text-muted mb-3"></i>
                    <p class="text-muted">No workflows found</p>
                </div>
            `;
            return;
        }

        container.innerHTML = workflows.map(workflow => `
            <div class="workflow-card border rounded p-3 mb-3">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <h6 class="mb-1">${this.escapeHtml(workflow.name)}</h6>
                        <p class="text-muted mb-2">${this.escapeHtml(workflow.description || 'No description')}</p>
                        <small class="text-muted">
                            Created: ${this.formatDate(workflow.createdAt)} | 
                            Updated: ${this.formatDate(workflow.updatedAt)}
                        </small>
                    </div>
                    <div class="ms-3">
                        <span class="badge ${workflow.isActive ? 'bg-success' : 'bg-secondary'} status-badge mb-2">
                            ${workflow.isActive ? 'Active' : 'Inactive'}
                        </span>
                        <div class="btn-group-vertical" role="group">
                            <button class="btn btn-outline-primary btn-sm" onclick="dashboard.executeWorkflow(${workflow.id})">
                                <i class="fas fa-play"></i> Execute
                            </button>
                            <button class="btn btn-outline-info btn-sm" onclick="dashboard.viewWorkflowExecutions(${workflow.id})">
                                <i class="fas fa-history"></i> History
                            </button>
                            <button class="btn btn-outline-danger btn-sm" onclick="dashboard.deleteWorkflow(${workflow.id})">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    renderRecentExecutions(executions) {
        const container = document.getElementById('recent-executions');
        
        if (executions.length === 0) {
            container.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-clock fa-2x text-muted mb-2"></i>
                    <p class="text-muted">No recent executions</p>
                </div>
            `;
            return;
        }

        container.innerHTML = executions.map(execution => `
            <div class="border-bottom pb-2 mb-2">
                <div class="d-flex justify-content-between align-items-center">
                    <h6 class="mb-1">${this.escapeHtml(execution.workflow?.name || 'Unknown Workflow')}</h6>
                    <span class="badge ${this.getStatusBadgeClass(execution.status)}">
                        ${this.getStatusText(execution.status)}
                    </span>
                </div>
                <div class="progress progress-container mb-1">
                    <div class="progress-bar ${this.getProgressBarClass(execution.status)}" 
                         style="width: ${execution.progressPercentage}%">
                    </div>
                </div>
                <small class="text-muted">
                    Started: ${this.formatDate(execution.startedAt)}
                    ${execution.durationMs ? `| Duration: ${execution.durationMs}ms` : ''}
                </small>
                ${execution.status === 1 ? `
                    <button class="btn btn-outline-warning btn-sm mt-1" 
                            onclick="dashboard.cancelExecution(${execution.id})">
                        <i class="fas fa-stop"></i> Cancel
                    </button>
                ` : ''}
            </div>
        `).join('');
    }

    renderPagination(data, containerId, onPageClick) {
        const container = document.getElementById(containerId);
        
        if (data.totalPages <= 1) {
            container.innerHTML = '';
            return;
        }

        const pages = [];
        const currentPage = data.page;
        const totalPages = data.totalPages;

        // Previous button
        pages.push(`
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="event.preventDefault(); ${currentPage > 1 ? `(${onPageClick})(${currentPage - 1})` : ''}">
                    Previous
                </a>
            </li>
        `);

        // Page numbers
        for (let i = Math.max(1, currentPage - 2); i <= Math.min(totalPages, currentPage + 2); i++) {
            pages.push(`
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="event.preventDefault(); (${onPageClick})(${i})">${i}</a>
                </li>
            `);
        }

        // Next button
        pages.push(`
            <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="event.preventDefault(); ${currentPage < totalPages ? `(${onPageClick})(${currentPage + 1})` : ''}">
                    Next
                </a>
            </li>
        `);

        container.innerHTML = `<ul class="pagination">${pages.join('')}</ul>`;
    }

    async executeWorkflow(workflowId) {
        try {
            const response = await fetch(`${this.baseUrl}/workflows/${workflowId}/execute`, {
                method: 'POST'
            });
            
            if (response.ok) {
                this.showSuccess('Workflow execution started');
                await this.loadRecentExecutions();
                await this.loadStats();
            } else {
                const error = await response.text();
                this.showError(`Failed to start workflow: ${error}`);
            }
        } catch (error) {
            console.error('Error executing workflow:', error);
            this.showError('Failed to execute workflow');
        }
    }

    async cancelExecution(executionId) {
        try {
            const response = await fetch(`${this.baseUrl}/executions/${executionId}/cancel`, {
                method: 'POST'
            });
            
            if (response.ok) {
                this.showSuccess('Execution cancelled');
                await this.loadRecentExecutions();
                await this.loadStats();
            } else {
                this.showError('Failed to cancel execution');
            }
        } catch (error) {
            console.error('Error cancelling execution:', error);
            this.showError('Failed to cancel execution');
        }
    }

    async deleteWorkflow(workflowId) {
        if (!confirm('Are you sure you want to delete this workflow?')) {
            return;
        }

        try {
            const response = await fetch(`${this.baseUrl}/workflows/${workflowId}`, {
                method: 'DELETE'
            });
            
            if (response.ok) {
                this.showSuccess('Workflow deleted');
                await this.loadWorkflows(this.currentPage);
                await this.loadStats();
            } else {
                this.showError('Failed to delete workflow');
            }
        } catch (error) {
            console.error('Error deleting workflow:', error);
            this.showError('Failed to delete workflow');
        }
    }

    viewWorkflowExecutions(workflowId) {
        // This could open a modal or navigate to a detailed view
        alert(`View executions for workflow ${workflowId} - Feature coming soon!`);
    }

    showCreateWorkflowModal() {
        const modal = new bootstrap.Modal(document.getElementById('createWorkflowModal'));
        modal.show();
    }

    async createWorkflow() {
        const form = document.getElementById('create-workflow-form');
        const formData = new FormData(form);
        
        const workflow = {
            name: document.getElementById('workflow-name').value,
            description: document.getElementById('workflow-description').value,
            isActive: document.getElementById('workflow-active').checked
        };

        if (!workflow.name.trim()) {
            this.showError('Workflow name is required');
            return;
        }

        try {
            const response = await fetch(`${this.baseUrl}/workflows`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(workflow)
            });
            
            if (response.ok) {
                this.showSuccess('Workflow created successfully');
                const modal = bootstrap.Modal.getInstance(document.getElementById('createWorkflowModal'));
                modal.hide();
                form.reset();
                await this.loadWorkflows(this.currentPage);
                await this.loadStats();
            } else {
                const error = await response.text();
                this.showError(`Failed to create workflow: ${error}`);
            }
        } catch (error) {
            console.error('Error creating workflow:', error);
            this.showError('Failed to create workflow');
        }
    }

    refreshDashboard() {
        const refreshIcon = document.getElementById('refresh-icon');
        refreshIcon.classList.add('spinning');
        
        this.loadDashboardData().finally(() => {
            refreshIcon.classList.remove('spinning');
        });
    }

    startAutoRefresh() {
        // Refresh every 30 seconds
        this.refreshInterval = setInterval(() => {
            this.loadStats();
            this.loadRecentExecutions();
        }, 30000);
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    // Utility methods
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    formatDate(dateString) {
        return new Date(dateString).toLocaleString();
    }

    getStatusBadgeClass(status) {
        const classes = {
            0: 'bg-secondary',  // Pending
            1: 'bg-info',       // Running
            2: 'bg-success',    // Completed
            3: 'bg-danger',     // Failed
            4: 'bg-warning'     // Cancelled
        };
        return classes[status] || 'bg-secondary';
    }

    getStatusText(status) {
        const texts = {
            0: 'Pending',
            1: 'Running',
            2: 'Completed',
            3: 'Failed',
            4: 'Cancelled'
        };
        return texts[status] || 'Unknown';
    }

    getProgressBarClass(status) {
        const classes = {
            0: 'bg-secondary',  // Pending
            1: 'bg-info',       // Running
            2: 'bg-success',    // Completed
            3: 'bg-danger',     // Failed
            4: 'bg-warning'     // Cancelled
        };
        return classes[status] || 'bg-secondary';
    }

    showError(message) {
        this.showToast(message, 'danger');
    }

    showSuccess(message) {
        this.showToast(message, 'success');
    }

    showToast(message, type) {
        // Create a simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
        `;
        document.body.appendChild(toast);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 5000);
    }
}

// Global dashboard instance
const dashboard = new WorkflowDashboard();

// Global functions for HTML onclick handlers
function refreshDashboard() {
    dashboard.refreshDashboard();
}

function showCreateWorkflowModal() {
    dashboard.showCreateWorkflowModal();
}

function createWorkflow() {
    dashboard.createWorkflow();
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    dashboard.stopAutoRefresh();
});