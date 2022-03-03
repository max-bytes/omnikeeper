import { useState } from 'react';

// TODO: move
// TODO: use useLocalStorage hook
export function useAttributeSegmentsToggler(segmentNames) {
    const [openAttributeSegments, setOpenAttributeSegmentsState] = useState(localStorage.getItem('openAttributeSegments') ? JSON.parse(localStorage.getItem('openAttributeSegments')) : [] );
    const setOpenAttributeSegments = (openAttributeSegments) => {
      setOpenAttributeSegmentsState(openAttributeSegments);
      localStorage.setItem('openAttributeSegments', JSON.stringify(openAttributeSegments));
    }
    const [expanded, setExpanded] = useState(false);
    const toggleExpandCollapseAll = () => {
      const newOpenAttributeSegments = expanded ? [] : segmentNames;
      setOpenAttributeSegments(newOpenAttributeSegments);
      setExpanded(!expanded);
    };
    const isSegmentActive = (key) => openAttributeSegments.includes(key);
  
    return [setOpenAttributeSegments, isSegmentActive, toggleExpandCollapseAll];
  }
  