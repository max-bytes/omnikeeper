import { useState } from 'react';

// TODO: move
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
    const toggleSegment = (index) => {
      if (openAttributeSegments.indexOf(index) === -1)
        setOpenAttributeSegments([...openAttributeSegments, index]);
      else
        setOpenAttributeSegments(openAttributeSegments.filter(i => i !== index));
    };
    const isSegmentActive = (key) => openAttributeSegments.includes(key);
  
    return [toggleSegment, isSegmentActive, toggleExpandCollapseAll];
  }
  