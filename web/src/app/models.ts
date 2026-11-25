export interface Passage {
  index: number;
  score: number;
  text: string;
}

export interface Step {
  kind: string;
  text: string;
}

export interface ChatResponse {
  answer: string;
  passages: Passage[];
  steps: Step[];
}

export interface HistoryItem {
  id: number;
  question: string;
  answer: string;
  passages: Passage[];
  createdAtUtc: string;
}