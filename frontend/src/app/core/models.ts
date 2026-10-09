export type Role = 'Admin' | 'Agent' | 'Customer';

export interface User { id: number; fullName: string; email: string; role: Role; }
export interface AuthResponse { token: string; user: User; }

export interface TicketListItem {
  id: number; title: string; status: string; priority: string; category: string; sentiment: string;
  createdByName: string; assignedToName: string | null; createdAt: string; updatedAt: string;
}

export interface Comment {
  id: number; authorId: number; authorName: string; authorRole: string;
  body: string; isInternal: boolean; createdAt: string;
}

export interface KbSuggestion { id: number; title: string; snippet: string; }

export interface TicketDetail {
  id: number; title: string; description: string; status: string; priority: string; category: string; sentiment: string;
  aiSummary: string; aiSuggestedReply: string; aiSource: string;
  createdById: number; createdByName: string; assignedToId: number | null; assignedToName: string | null;
  createdAt: string; updatedAt: string; resolvedAt: string | null;
  comments: Comment[]; suggestions: KbSuggestion[];
}

export interface KbArticle { id: number; title: string; content: string; tags: string; createdAt: string; }
export interface LabelCount { label: string; count: number; }

export interface Dashboard {
  total: number; open: number; inProgress: number; resolved: number; closed: number;
  avgResolutionHours: number; urgentOpen: number; negativeOpen: number;
  byCategory: LabelCount[]; byPriority: LabelCount[]; last7Days: LabelCount[];
}

export const STATUSES = ['Open', 'InProgress', 'Resolved', 'Closed'];
export const PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'];
export const CATEGORIES = ['Billing', 'Technical', 'Account', 'Feature Request', 'General'];
